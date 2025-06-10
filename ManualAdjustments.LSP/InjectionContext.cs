using ManualAdjustments.LSP.Messages;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace ManualAdjustments.LSP;

internal delegate Task<ResponseMessage> RequestMessageHandler(RequestMessage request);
internal delegate Task NotificationMessageHandler(NotificationMessage notification);

internal class InjectionContext
{
	public static InjectionContext Shared { get; } = new();

	private readonly Dictionary<Type, dynamic> _injectables = [];
	private readonly Dictionary<string, RequestMessageHandler> _reqHandlers = [];
	private readonly Dictionary<string, NotificationMessageHandler> _noteHandlers = [];

	public void Add<T>(T instance) where T : notnull => _injectables[typeof(T)] = instance;

	public void Add<T>() where T : notnull
	{
		Type type = typeof(T);

		ConstructorInfo ctor = type.GetConstructors()
			.Where(ctor => ctor.GetParameters().All(p => _injectables.ContainsKey(p.ParameterType)))
			.MaxBy(static ctor => ctor.GetParameters().Length) ?? throw new ArgumentException("Could not find suitable constructor for type.", nameof(T));

		object[] ctorParams = [..ctor.GetParameters().Select(param => _injectables[param.ParameterType])];

		if (ctor.Invoke(ctorParams) is not T instance)
			throw new ArgumentException("Invocation of constructor failed for type.", nameof(T));

		Add(instance);
	}

	public T Get<T>() => _injectables[typeof(T)];

	public T? Get<T>(bool errorIfNotFound) =>
		errorIfNotFound
		? Get<T>()
		: _injectables.TryGetValue(typeof(T), out dynamic? instance) ? instance : null;

	public bool TryGet<T>([NotNullWhen(true)] out T? instance)
	{
		instance = default;

		if (!_injectables.TryGetValue(typeof(T), out dynamic? inst))
			return false;

		instance = inst;
		return true;
	}

	public void AddHandler(string method, RequestMessageHandler handler) =>
		_reqHandlers[method] = handler;

	public void AddHandler(string method, NotificationMessageHandler handler) =>
		_noteHandlers[method] = handler;

	public RequestMessageHandler? GetReqHandler(string method) =>
		_reqHandlers.TryGetValue(method, out var retval)
		? retval
		: null;

	public NotificationMessageHandler? GetNoteHandler(string method) =>
		_noteHandlers.TryGetValue(method, out var retval)
		? retval
		: null;

	public void LoadParamTypes()
	{
		Dictionary<string, Type> paramTypes = [];

		foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
		{
			string method;

			if (type.IsInterface)
				continue;
			else if (type.GetInterface("IParams") is not null && type.GetInterface("IResult") is null)
			{
				PropertyInfo methodProp = type.GetProperty("Method", BindingFlags.Public | BindingFlags.Static) ?? throw new Exception();
				method = (string)methodProp.GetValue(null)!;


				if (type.GetInterface("IRequestParams") is not null)
				{
					// Register notification handler!
					Type requestCheckType = typeof(RequestMessage<>).MakeGenericType(type);

					AddHandler(method, req => {
						if (req.GetType() != requestCheckType)
							throw new ArgumentException($"Invalid params for {method} request handler!", nameof(req));

						dynamic castReq = Convert.ChangeType(req, requestCheckType);
						return castReq.Params.HandleAsync(req.Id);
					});
				}
				else if (type.GetInterface("INotificationParams") is not null)
				{
					// Register notification handler!
					Type notificationCheckType = typeof(NotificationMessage<>).MakeGenericType(type);

					AddHandler(method, (NotificationMessage note) => {
						if (note.GetType() != notificationCheckType)
							throw new ArgumentException($"Invalid params for {method} notification handler!", nameof(note));

						return ((dynamic)Convert.ChangeType(note, notificationCheckType)).Params.HandleAsync();
					});
				}
			}
			else
				continue;

			paramTypes.Add(method, type);
		}

		_injectables.Add(typeof(ImmutableDictionary<string, Type>), paramTypes.ToImmutableDictionary());
	}
}
