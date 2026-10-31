using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Tests.Models;

[TestFixture(TestOf = typeof(FileTransferParameters))]
public class FileTransferParametersTests
{
    [Test]
    public async Task ReadFileDataAsync_WithFileData_ReturnsItWithoutReadingThePath()
    {
        var parameters = new FileTransferParameters
        {
            FilePath = "not-a-real-file.bin",
            FileData = [1, 2, 3]
        };

        var data = await parameters.ReadFileDataAsync();

        Assert.That(data, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public async Task ReadFileDataAsync_WithOnlyAPath_ReadsTheFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, [4, 5]);
            var parameters = new FileTransferParameters { FilePath = path };

            var data = await parameters.ReadFileDataAsync();

            Assert.That(data, Is.EqualTo(new byte[] { 4, 5 }));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ReadFileDataAsync_WithNoFile_Throws()
    {
        var parameters = new FileTransferParameters();

        Assert.ThrowsAsync<InvalidOperationException>(() => parameters.ReadFileDataAsync());
    }
}
