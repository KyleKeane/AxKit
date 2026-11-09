using System.Text;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>The capped line read of the pipes (security audit S-56): a line is read whole up to 64 KB and refused past it.</summary>
public sealed class UserPipeTests
{
    [Fact]
    public void A_line_is_read_whole_and_a_longer_one_refused()
    {
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("hello\r\n" + new string('x', 70_000) + "\nafter\n")));
        Assert.Equal("hello", UserPipe.ReadLine(reader));
        Assert.Null(UserPipe.ReadLine(reader));
    }

    [Fact]
    public void The_last_line_without_an_end_comes_back_and_then_nothing()
    {
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("one")));
        Assert.Equal("one", UserPipe.ReadLine(reader));
        Assert.Null(UserPipe.ReadLine(reader));
    }
}