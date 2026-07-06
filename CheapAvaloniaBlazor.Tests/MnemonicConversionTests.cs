using CheapAvaloniaBlazor.Services.Backends;

namespace CheapAvaloniaBlazor.Tests;

public class MnemonicConversionTests
{
    [Theory]
    [InlineData("&File", "_File")]
    [InlineData("Save && Exit", "Save & Exit")]
    [InlineData("Snake_Case", "Snake__Case")]
    [InlineData("A&&B_C", "A&B__C")]
    [InlineData("&Open_File && Co", "_Open__File & Co")]
    [InlineData("Plain", "Plain")]
    public void Win32_mnemonics_convert_to_gtk_style(string win32Text, string expectedGtkText)
    {
        Assert.Equal(expectedGtkText, GtkMenuBarBackend.ConvertMnemonics(win32Text));
    }
}
