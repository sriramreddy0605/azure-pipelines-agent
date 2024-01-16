using Agent.Worker.Handlers.Helpers;

namespace Test.L0.Worker.Handlers;

public class OutputDataHelperL0
{
    [Theory]
    [InlineData("\u001b[31;1mThis is \u001b[36;1mCustom line \u001b[0mOf text\u001b[0m", "This is Custom line Of text")]
    public void Test_RemoveAnsiColorsFromLine(string inputLine, string expectedLine)
    {
        var actualLine = OutputDataHelper.RemoveAnsiColorsFromLine(inputLine);

        Assert.Equal(expectedLine, actualLine);
    }
}
