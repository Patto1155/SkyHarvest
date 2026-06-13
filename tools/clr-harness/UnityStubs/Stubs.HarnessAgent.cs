// Harness stub additions for game code that outpaced Stubs.UI.cs.
namespace UnityEngine.UI
{
    public partial class Text
    {
        public bool supportRichText { get; set; }
    }

    public partial class InputField
    {
        public Text? textComponent { get; set; }
        public LineType lineType { get; set; }

        public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
    }
}
