// UnityEngine stub — JsonUtility: real implementation via System.Text.Json + reflection
// so save/load tests genuinely pass.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace UnityEngine
{
    /// <summary>
    /// JsonUtility stub that uses reflection to serialize/deserialize public fields
    /// (matching Unity's behavior) so harness-level save/load tests genuinely work.
    /// Supports: primitives, string, struct/class with public fields, arrays, Lists,
    /// nested objects. Does NOT support Dictionaries (Unity doesn't either).
    /// </summary>
    public static class JsonUtility
    {
        public static string ToJson(object obj, bool prettyPrint = false)
        {
            if (obj == null) return "null";
            var sb = new StringBuilder();
            SerializeValue(obj, sb, prettyPrint ? 0 : -1);
            return sb.ToString();
        }

        public static T FromJson<T>(string json) => (T)Deserialize(json.Trim(), typeof(T))!;

        public static object? FromJson(string json, Type type) => Deserialize(json.Trim(), type);

        public static void FromJsonOverwrite(string json, object obj)
        {
            if (obj == null) return;
            var dict = ParseObject(json.Trim());
            ApplyDict(dict, obj);
        }

        // ---- Serialization ----

        private static void SerializeValue(object? value, StringBuilder sb, int indent)
        {
            if (value == null) { sb.Append("null"); return; }
            var type = value.GetType();

            if (type == typeof(string)) { sb.Append('"'); sb.Append(((string)value).Replace("\\", "\\\\").Replace("\"", "\\\"")); sb.Append('"'); return; }
            if (type == typeof(bool)) { sb.Append((bool)value ? "true" : "false"); return; }
            if (type.IsPrimitive) { sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)); return; }
            if (type.IsEnum) { sb.Append((int)value); return; }

            // Array / List<T>
            if (type.IsArray)
            {
                var arr = (Array)value;
                sb.Append('[');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    SerializeValue(arr.GetValue(i), sb, indent >= 0 ? indent + 1 : -1);
                }
                sb.Append(']');
                return;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = (System.Collections.IList)value;
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    SerializeValue(list[i], sb, indent >= 0 ? indent + 1 : -1);
                }
                sb.Append(']');
                return;
            }

            // Object: serialize public fields (matching Unity's behavior — public fields only, no properties)
            sb.Append('{');
            bool first = true;
            foreach (var field in GetSerializableFields(type))
            {
                if (!first) sb.Append(',');
                first = false;
                if (indent >= 0) { sb.Append('\n'); Indent(sb, indent + 1); }
                sb.Append('"'); sb.Append(field.Name); sb.Append("\":");
                if (indent >= 0) sb.Append(' ');
                SerializeValue(field.GetValue(value), sb, indent >= 0 ? indent + 1 : -1);
            }
            if (indent >= 0 && !first) { sb.Append('\n'); Indent(sb, indent); }
            sb.Append('}');
        }

        private static void Indent(StringBuilder sb, int level) { for (int i = 0; i < level; i++) sb.Append("  "); }

        private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            // Unity serializes: public fields, or private fields with [SerializeField]
            var fields = new List<FieldInfo>();
            var t = type;
            while (t != null && t != typeof(object))
            {
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.IsPublic && !f.IsStatic) fields.Add(f);
                    else if (!f.IsPublic && f.GetCustomAttribute<UnityEngine.SerializeFieldAttribute>() != null) fields.Add(f);
                }
                t = t.BaseType;
            }
            return fields;
        }

        // ---- Deserialization ----

        private static object? Deserialize(string json, Type type)
        {
            if (json == "null" || string.IsNullOrEmpty(json)) return type.IsValueType ? Activator.CreateInstance(type) : null;

            if (type == typeof(string)) return json.StartsWith('"') ? ParseString(json) : json;
            if (type == typeof(bool)) return json == "true";
            if (type == typeof(int)) return int.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(long)) return long.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(float)) return float.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(uint)) return uint.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(ulong)) return ulong.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(byte)) return byte.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(short)) return short.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
            if (type.IsEnum) { if (int.TryParse(json, out int ev)) return Enum.ToObject(type, ev); return Enum.Parse(type, json.Trim('"')); }

            if (type.IsArray)
            {
                var elemType = type.GetElementType()!;
                var items = ParseArray(json);
                var arr = Array.CreateInstance(elemType, items.Count);
                for (int i = 0; i < items.Count; i++) arr.SetValue(Deserialize(items[i], elemType), i);
                return arr;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elemType = type.GetGenericArguments()[0];
                var items = ParseArray(json);
                var list = (System.Collections.IList)Activator.CreateInstance(type)!;
                foreach (var item in items) list.Add(Deserialize(item, elemType));
                return list;
            }

            // Object
            var dict2 = ParseObject(json);
            var obj = type.IsValueType ? Activator.CreateInstance(type)! : Activator.CreateInstance(type)!;
            ApplyDict(dict2, obj);
            return obj;
        }

        private static void ApplyDict(Dictionary<string, string> dict, object obj)
        {
            var type = obj.GetType();
            bool isBoxed = type.IsValueType;
            object boxed = obj;

            foreach (var field in GetSerializableFields(type))
            {
                if (dict.TryGetValue(field.Name, out var raw))
                {
                    try
                    {
                        var val = Deserialize(raw, field.FieldType);
                        if (isBoxed)
                        {
                            field.SetValue(boxed, val);
                        }
                        else
                        {
                            field.SetValue(obj, val);
                        }
                    }
                    catch { /* skip undeserializable fields */ }
                }
            }
            // For value types, we can't mutate obj in place via reflection after boxing.
            // Callers using FromJsonOverwrite on structs get best-effort.
        }

        // ---- Mini JSON parser ----

        private static string ParseString(string json)
        {
            json = json.Trim();
            if (!json.StartsWith('"')) return json;
            return json.Substring(1, json.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
        }

        private static Dictionary<string, string> ParseObject(string json)
        {
            var result = new Dictionary<string, string>();
            json = json.Trim();
            if (!json.StartsWith('{')) return result;
            json = json.Substring(1, json.Length - 2).Trim();

            int pos = 0;
            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length) break;
                if (json[pos] != '"') break;

                // Key
                string key = ReadString(json, ref pos);
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':') break;
                pos++; // skip ':'
                SkipWhitespace(json, ref pos);

                // Value
                string val = ReadValue(json, ref pos);
                result[key] = val;

                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') pos++;
            }
            return result;
        }

        private static List<string> ParseArray(string json)
        {
            var result = new List<string>();
            json = json.Trim();
            if (!json.StartsWith('[')) return result;
            json = json.Substring(1, json.Length - 2).Trim();

            int pos = 0;
            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length) break;
                string val = ReadValue(json, ref pos);
                result.Add(val);
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') pos++;
            }
            return result;
        }

        private static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
        }

        private static string ReadString(string s, ref int pos)
        {
            if (s[pos] != '"') return string.Empty;
            pos++;
            int start = pos;
            while (pos < s.Length && !(s[pos] == '"' && s[pos - 1] != '\\')) pos++;
            string result = s.Substring(start, pos - start).Replace("\\\"", "\"");
            if (pos < s.Length) pos++; // skip closing quote
            return result;
        }

        private static string ReadValue(string s, ref int pos)
        {
            if (pos >= s.Length) return string.Empty;
            char c = s[pos];

            if (c == '"') { int start = pos; pos++; while (pos < s.Length && !(s[pos] == '"' && s[pos-1] != '\\')) pos++; if (pos < s.Length) pos++; return s.Substring(start, pos - start); }
            if (c == '{' || c == '[')
            {
                char open = c, close = c == '{' ? '}' : ']';
                int depth = 0, start = pos;
                while (pos < s.Length)
                {
                    if (s[pos] == '"') { pos++; while (pos < s.Length && !(s[pos] == '"' && s[pos-1] != '\\')) pos++; if (pos < s.Length) pos++; continue; }
                    if (s[pos] == open) depth++;
                    else if (s[pos] == close) { depth--; if (depth == 0) { pos++; break; } }
                    pos++;
                }
                return s.Substring(start, pos - start);
            }
            // Number / bool / null
            {
                int start = pos;
                while (pos < s.Length && s[pos] != ',' && s[pos] != '}' && s[pos] != ']' && !char.IsWhiteSpace(s[pos])) pos++;
                return s.Substring(start, pos - start).Trim();
            }
        }
    }
}
