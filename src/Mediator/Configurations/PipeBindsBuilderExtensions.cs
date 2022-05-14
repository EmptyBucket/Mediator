using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;

namespace Mediator.Configurations;

public static class PipeBindsBuilderExtensions
{
    public static IPipeBinder Bind(this IPipeBinder pipeBinder, Type pipeType, string pipeName = "") =>
        pipeBinder.Bind(pipeType, pipeType, pipeName);

    public static IPipeBinder Bind<TPipe>(this IPipeBinder pipeBinder, string pipeName = "") =>
        pipeBinder.Bind(typeof(TPipe), pipeName);

    public static IPipeBinder BindInterfaces(this IPipeBinder pipeBinder, Type pipeType, string pipeName = "") =>
        pipeType.GetInterfaces()
            .Where(i => i.IsAssignableTo(typeof(IPubPipe)) || i.IsAssignableTo(typeof(IReqPipe)))
            .Aggregate(pipeBinder, (b, i) => b.Bind(i, pipeType, pipeName));

    public static IPipeBinder BindInterfaces<TPipe>(this IPipeBinder pipeBinder, string pipeName = "") =>
        pipeBinder.BindInterfaces(typeof(TPipe), pipeName);
}