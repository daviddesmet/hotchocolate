using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Driver;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    [Obsolete]
    public class MongoSortingObjectTests : IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public MongoSortingObjectTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public async Task GetItems_NoSorting_AllItems_Are_Returned_Unsorted()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoCollection<Parent>>(sp =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                    collection.InsertMany(new[]
                    {
                        new Parent { Model = new Model { Foo = "abc", Bar = 1, Baz = true } },
                        new Parent { Model = new Model { Foo = "def", Bar = 2, Baz = false } }
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ items { model { foo } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_DescSorting_AllItems_Are_Returned_DescSortedOnNullable()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoCollection<Parent>>(sp =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                    collection.InsertMany(new[]
                    {
                        new Parent {
                            Model = new Model {
                                Foo = "def", Bar = 1, Baz = true, BazNullable= 1 } },
                        new Parent {
                            Model = new Model {
                                Foo = "abc", Bar = 2, Baz = false, BazNullable= 2 } },
                        new Parent {
                            Model = new Model {
                                Foo = "abc", Bar = 2, Baz = false, BazNullable= null } }
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();


            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                "{ items(order_by: { model: { bazNullable: DESC } }) { model { bazNullable } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_DescSorting_AllItems_Are_Returned_DescSortedOnNullableObject()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoCollection<Parent>>(sp =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                    collection.InsertMany(new[]
                    {
                        new Parent {
                            Model = new Model {
                            Foo = "def", Bar = 1, Baz = true, BazNullable= 1 } },
                        new Parent {
                            Model = new Model {
                                Foo = "abc", Bar = 2, Baz = false, BazNullable= 2 } },
                        new Parent { }
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();


            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                 "{ items(order_by: { model: { bazNullable: DESC } }) { model { bazNullable } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.MatchSnapshot();
        }


        [Fact]
        public async Task GetItems_DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton(_ =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                    collection.InsertMany(new[]
                    {
                        new Parent { Model = new Model { Foo = "abc", Bar = 1, Baz = true } },
                        new Parent { Model = new Model { Foo = "def", Bar = 2, Baz = false } }
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();


            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ items(order_by: { model: { foo: DESC } }) { model { foo } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_With_Paging__DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton(_ =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                    collection.InsertMany(new[]
                    {
                        new Parent { Model = new Model { Foo = "abc", Bar = 1, Baz = true } },
                        new Parent { Model = new Model { Foo = "def", Bar = 2, Baz = false } }
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "{ paging(order_by: { model: { foo: DESC } }) " +
                    "{ nodes { model { foo } } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.MatchSnapshot();
        }


        [Fact]
        public async Task GetItems_OnRenamedField_DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoCollection<Parent>>(sp =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                    collection.InsertMany(new[]
                    {
                        new Parent { Model = new Model { Foo = "abc", Bar = 1, Baz = true } },
                        new Parent { Model = new Model { Foo = "def", Bar = 2, Baz = false } }
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ items(order_by: { model: { qux: DESC } }) { model { bar } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.MatchSnapshot();
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");

                descriptor.Field("items")
                    .Type<ListType<ParentType>>()
                    .UseSorting<ParentSortInputType>()
                    .Resolve(ctx => ctx.Service<IMongoCollection<Parent>>().AsQueryable());

                descriptor.Field("paging")
                    .UsePaging<ParentType>()
                    .UseSorting<ParentSortInputType>()
                    .Resolve(ctx => ctx.Service<IMongoCollection<Parent>>().AsQueryable());
            }
        }

        public class ParentSortInputType : SortInputType<Parent>
        {
            protected override void Configure(ISortInputTypeDescriptor<Parent> descriptor)
            {
                descriptor.SortableObject(m => m.Model).Type<ModelSortInputType>();
            }
        }
        public class ModelSortInputType : SortInputType<Model>
        {
            protected override void Configure(ISortInputTypeDescriptor<Model> descriptor)
            {
                descriptor.Sortable(m => m.Bar).Name("qux");
            }
        }

        public class ModelType : ObjectType<Model>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Model> descriptor)
            {
                descriptor.Field(t => t.Id)
                    .Type<IdType>()
                    .Resolve(c => c.Parent<Model>().Id);
            }
        }

        public class ParentType : ObjectType<Parent>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Parent> descriptor)
            {
                descriptor.Field(t => t.Model)
                    .Type<ModelType>();
                descriptor.Field(t => t.Id)
                    .Type<IdType>()
                    .Resolve(c => c.Parent<Model>().Id);
            }
        }
        public class Parent
        {
            public ObjectId Id { get; set; }
            public Model Model { get; set; }
        }

        public class Model
        {
            public ObjectId Id { get; set; }
            public string Foo { get; set; }
            public int Bar { get; set; }
            public bool Baz { get; set; }
            public int? BazNullable { get; set; }
        }
    }
}
