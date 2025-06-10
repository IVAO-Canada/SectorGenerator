using ManualAdjustments.LSP.Messages;

namespace ManualAdjustments.LSP.Types;

internal interface IParams { };

internal interface IRequestParams : IParams
{
	public abstract static string Method { get; }
	public abstract Task<ResponseMessage> HandleAsync(int id);
}

internal interface INotificationParams : IParams
{
	public abstract static string Method { get; }
	public abstract Task HandleAsync();
}

internal interface IResult : IParams { }

internal interface IArrayResult<T> : IResult where T : IResult
{
	public T[] Items { get; init; }
}
