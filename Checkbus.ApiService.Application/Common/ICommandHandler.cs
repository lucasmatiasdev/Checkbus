using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Checkbus.ApiService.Application.Common
{
    public interface ICommandHandler<TCommand, TResult>
    {
        Task<TResult> HandleAsync(TCommand command);
    }
}
