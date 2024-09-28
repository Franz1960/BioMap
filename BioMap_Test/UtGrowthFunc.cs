namespace BioMap_Test
{
  public class UtGrowthFunc
  {
    [SetUp]
    public void Setup() {
    }
    private readonly DateTime[] SampleBirthDates = new DateTime[] {
      DateTime.Parse("2010-05-15"),
      DateTime.Parse("2018-05-31"),
      DateTime.Parse("2018-06-01"),
      DateTime.Parse("2018-06-02"),
      DateTime.Parse("2018-06-30"),
      DateTime.Parse("2018-09-04"),
      DateTime.Parse("2018-12-24"),
      DateTime.Parse("2019-03-13"),
      DateTime.Parse("2019-07-15"),
      DateTime.Parse("2020-02-15"),
      DateTime.Parse("2021-06-17"),
      DateTime.Parse("2023-08-15"),
      DateTime.Parse("2036-05-03"),
    };
    [Test]
    public void TestMonotony() {
      var gf1 = new GrowthFunc() {
        DateOfBirth = DateTime.Parse("2018-06-01"),
      };
      for (int idx1 = 1; idx1 < this.SampleBirthDates.Length; idx1++) {
        var l0 = gf1.GetSize(this.SampleBirthDates[idx1 - 1]);
        var l1 = gf1.GetSize(this.SampleBirthDates[idx1]);
        Assert.GreaterOrEqual(l1, l0);
      }
    }
  }
}
