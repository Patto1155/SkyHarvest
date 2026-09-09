// Assets/Tests/EditMode/WelcomeBackTests.cs
using NUnit.Framework;
using SkyHarvest.Sim;
using SkyHarvest.UI;

[TestFixture]
public class WelcomeBackTests
{
    [Test]
    public void Duration_Formats_Human_Readably()
    {
        Assert.AreEqual("45s",    OfflineReport.FormatDuration(45f));
        Assert.AreEqual("5m",     OfflineReport.FormatDuration(300f));
        Assert.AreEqual("2h",     OfflineReport.FormatDuration(7200f));
        Assert.AreEqual("2h 14m", OfflineReport.FormatDuration(7200f + 14 * 60f));
    }

    [Test]
    public void Body_Lists_Harvests_By_Display_Name_And_Buffer_Events()
    {
        var r = new OfflineReport { ElapsedSeconds = 3600 };
        r.RecordHarvest("wheat", 6);
        r.RecordHarvest("wheat", 2);
        r.Replanted = 3;
        r.Clock = 1500f; r.MarkStorageFull();

        string body = WelcomeBackUI.BuildBody(r);

        StringAssert.Contains("+8 Wheat", body);
        StringAssert.Contains("3 plots replanted", body);
        StringAssert.Contains("Storage filled after 25m", body);
        Assert.IsTrue(r.HasAnythingToShow);
    }

    [Test]
    public void Quiet_Report_Says_So()
    {
        var r = new OfflineReport { ElapsedSeconds = 120 };
        StringAssert.Contains("waited quietly", WelcomeBackUI.BuildBody(r));
    }

    [Test]
    public void Build_Menu_Scroll_Keeps_Selection_Visible()
    {
        // 18 structures, 12 rows (Bootstrap): top of list pins to 0, tail pins to 6.
        Assert.AreEqual(0, BuildMenuUI.ScrollOffset(0, 18, 12));
        Assert.AreEqual(0, BuildMenuUI.ScrollOffset(5, 18, 12));
        Assert.AreEqual(4, BuildMenuUI.ScrollOffset(10, 18, 12));
        Assert.AreEqual(6, BuildMenuUI.ScrollOffset(17, 18, 12));
        Assert.AreEqual(0, BuildMenuUI.ScrollOffset(3, 5, 12));
    }

    [Test]
    public void Buffer_Marks_Only_Record_First_Occurrence()
    {
        var r = new OfflineReport();
        r.Clock = 60f; r.MarkWaterEmpty();
        r.Clock = 600f; r.MarkWaterEmpty();
        Assert.AreEqual(60f, r.WaterEmptyAt, 0.001f);
    }
}
