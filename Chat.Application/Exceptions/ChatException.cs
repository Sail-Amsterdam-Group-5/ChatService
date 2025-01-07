namespace Chat.Application.Exceptions;

public class ChatException : Exception
{
    public ChatException() { }
    public ChatException(string message) : base(message) { }
    public ChatException(string message, Exception inner) : base(message, inner) { }
}

public class ChatNotFoundException : ChatException
{
    public ChatNotFoundException(string chatId)
        : base($"Chat with ID {chatId} was not found.") { }
}

public class UnauthorizedChatAccessException : ChatException
{
    public UnauthorizedChatAccessException(string userId, string chatId)
        : base($"User {userId} is not authorized to access chat {chatId}.") { }
}

public class MessageNotFoundException : ChatException
{
    public MessageNotFoundException(string messageId)
        : base($"Message with ID {messageId} was not found.") { }
}

public class InvalidOperationChatException : ChatException
{
    public InvalidOperationChatException(string message) : base(message) { }
}