using System.Net.Sockets;
using System.Text;
using FileServer;
using FileServer.Models;
using Microsoft.EntityFrameworkCore;
using FileServer.TCP;

CustomTcpListener listener = new(1488);

var optionBuilder = new DbContextOptionsBuilder<ApplicationContext>();
optionBuilder.UseSqlServer("Server=DESKTOP-837S6RV\\MSSQLSERVER01;Database=WebDatabase;Trusted_Connection=True;MultipleActiveResultSets=true; TrustServerCertificate=true");
var context = new ApplicationContext(optionBuilder.Options);

while (true)
{
    CustomTcpClient client = await listener.AcceptTcpClientAsync();
    Task.Run(async () => await ProcessClient(client));
}

async Task ProcessClient(CustomTcpClient client)
{
    NetworkStream stream = client.GetStream();

    byte[] aT = new byte[1];
    await stream.ReadAsync(aT, 0, 1);

    byte[] userId = new byte[1];
    await stream.ReadAsync(userId, 0, 1);

    byte[] fNameb = new byte[4];
    stream.Read(fNameb, 0, 4);
    int flength = BitConverter.ToInt32(fNameb);
    byte[] fbytes = new byte[flength];
    stream.Read(fbytes, 0, flength);

    byte[] contentLengthBuffer = new byte[sizeof(int)];
    await stream.ReadAsync(contentLengthBuffer, 0, sizeof(int));

    int contentLength = BitConverter.ToInt32(contentLengthBuffer);
    byte[] dataBuffer = new byte[contentLength];
    await stream.ReadAsync(dataBuffer, 0, contentLength);

    ActionType actionType = (ActionType)aT[0];

    switch (actionType)
    {
        case ActionType.LoadFiles:
            {
                var item = new FileModel
                {
                    Name = Encoding.UTF8.GetString(fbytes),
                    Bytes = dataBuffer,
                    UserModelId = userId[0],
                    ShareToAll = 1
                };

                context.Files.Add(item);
                context.SaveChanges();
                break;
            }
    }
}

int ReadNextInt(byte[] dataBuffer, ref int dataBufferOffset)
{
    byte[] intBytes = dataBuffer[dataBufferOffset..(dataBufferOffset += sizeof(int))];
    return BitConverter.ToInt32(intBytes);
}

string ReadNextString(byte[] dataBuffer, ref int dataBufferOffset)
{
    int length = ReadNextInt(dataBuffer, ref dataBufferOffset);
    byte[] stringBytes = dataBuffer[dataBufferOffset..(dataBufferOffset += length)];
    return Encoding.UTF8.GetString(stringBytes);
}

void WriteNextInt(byte[] dataBuffer, ref int dataBufferOffset, int value)
{
    byte[] intBytes = BitConverter.GetBytes(value);
    Buffer.BlockCopy(intBytes, 0, dataBuffer, dataBufferOffset, sizeof(int));
    dataBufferOffset += sizeof(int);
}

void WriteNextString(byte[] dataBuffer, ref int dataBufferOffset, string value)
{
    byte[] stringBytes = Encoding.UTF8.GetBytes(value);
    int length = stringBytes.Length;
    WriteNextInt(dataBuffer, ref dataBufferOffset, length);
    Buffer.BlockCopy(stringBytes, 0, dataBuffer, dataBufferOffset, length);
    dataBufferOffset += length;
}
