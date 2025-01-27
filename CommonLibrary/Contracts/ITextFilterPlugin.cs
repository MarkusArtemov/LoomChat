using De.Hsfl.LoomChat.Common.Contracts;

public interface ITextFilterPlugin : IChatPlugin
{
    string OnBeforeSend(string message);
    string OnBeforeReceive(string message);
}