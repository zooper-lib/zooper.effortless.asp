using ZEA.Techniques.ADTs;
using ZEA.Techniques.ADTs.Helpers;

namespace ZEA.Applications.Workflows;

public interface IPreProcessBehavior<in TRequest, TError>
{
	Task<Either<Success, TError>> ExecuteAsync(
		TRequest request,
		CancellationToken cancellationToken);
}