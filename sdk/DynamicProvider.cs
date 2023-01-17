// Copyright 2016-2023, Pulumi Corporation

using Pulumi.Experimental.Provider;

namespace Pulumi.Experimental.Dynamic
{
    public abstract class DynamicProvider
    {
        public virtual Task<CheckResponse> Check(CheckRequest request, CancellationToken ct)
        {
            return Task.FromResult(new CheckResponse()
            {
                Inputs = request.News
            });
        }

        public virtual Task<DiffResponse> Diff(DiffRequest request, CancellationToken ct)
        {
            return Task.FromResult(new DiffResponse()
            {

            });
        }

        public virtual Task<CreateResponse> Create(CreateRequest request, CancellationToken ct)
        {
            return Task.FromResult(new CreateResponse()
            {
                Properties = request.Properties
            });
        }

        public virtual Task<UpdateResponse> Update(UpdateRequest request, CancellationToken ct)
        {
            return Task.FromResult(new UpdateResponse()
            {
                Properties = request.News
            });
        }

        public virtual Task Delete(DeleteRequest request, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
