using ManualAdjustments.LSP.Messages;

namespace ManualAdjustments.LSP.Types;

internal interface IParams { };

internal interface IRequestParams<T> : IParams where T : ResponseMessage
{
	public abstract static string Method { get; }
	public abstract Task<T> HandleAsync(int id);
}

internal interface INotificationParams : IParams
{
	public abstract static string Method { get; }
	public abstract Task HandleAsync();
}

internal interface IResult : IParams { }
