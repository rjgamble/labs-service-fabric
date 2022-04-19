using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Customers.DataAccess;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Customers
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class Customers : StatefulService
    {
        public Customers(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        var builder = WebApplication.CreateBuilder();

                        builder.Services
                                    .AddSingleton<StatefulServiceContext>(serviceContext)
                                    .AddSingleton<IReliableStateManager>(this.StateManager);
                        builder.WebHost
                                    .UseKestrel()
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    .UseUrls(url);
                        
                        // Add services to the container.
                        builder.Services.AddSingleton(new Store());
                        builder.Services.AddTransient<ICustomerRepository, CustomerRepository>();

                        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                        builder.Services.AddEndpointsApiExplorer();
                        builder.Services.AddSwaggerGen();
                        
                        var app = builder.Build();
                        
                        // Configure the HTTP request pipeline.
                        app.UseSwagger();
                        app.UseSwaggerUI();

                        app.MapGet("/customers/{id}", ([FromRoute] string id, ICustomerRepository customers) =>
                        {
                            var customer = customers.GetById(id);
                            if (customer == null)
                            {
                                return Results.NotFound();
                            }

                            return Results.Ok(customer);
                        })
                            .Produces(StatusCodes.Status404NotFound)
                            .Produces<Customer>(statusCode: StatusCodes.Status200OK)
                            .WithName("GetCustomerById");

                        app.MapPost("/customers", ([FromBody] CreateCustomer request, ICustomerRepository customers) =>
                        {
                            var customer = customers.Create(request);
                            if (customer == null)
                            {
                                return Results.StatusCode(500);
                            }

                            return Results.Created($"/customers/{customer.Id}", customer);
                        })
                            .Produces(StatusCodes.Status500InternalServerError)
                            .Produces<Customer>(statusCode: StatusCodes.Status201Created)
                            .WithName("CreateCustomer");

                        app.MapPut("/customer/{id}", ([FromRoute] string id, [FromBody] UpdateCustomer request, ICustomerRepository customers) =>
                        {
                            var customer = customers.Update(id, request.Name, request.Level);
                            if (customer == null)
                            {
                                return Results.NotFound();
                            }

                            return Results.Ok(customer);
                        })
                            .Produces(StatusCodes.Status404NotFound)
                            .Produces<Customer>(statusCode: StatusCodes.Status200OK)
                            .WithName("UpdateCustomer");

                        app.MapMethods("/customer/{id}", new [] { "PATCH"}, ([FromRoute] string id, [FromBody] PatchCustomer request, ICustomerRepository customers) =>
                        {
                            var customer = customers.Update(id, request.Name, request.Level);
                            if (customer == null)
                            {
                                return Results.NotFound();
                            }

                            return Results.Ok(customer);
                        })
                            .Produces(StatusCodes.Status404NotFound)
                            .Produces<Customer>(statusCode: StatusCodes.Status200OK)
                            .WithName("PatchCustomer");

                        app.MapDelete("/customers/{id}", ([FromRoute] string id, ICustomerRepository customers) =>
                        {
                            var customer = customers.Delete(id);
                            if(customer == null)
                            {
                                return Results.NotFound();
                            }

                            return Results.NoContent();
                        })
                            .Produces(StatusCodes.Status404NotFound)
                            .Produces<Customer>(statusCode: StatusCodes.Status204NoContent)
                            .WithName("DeleteCustomer");

                        app.MapGet("/customers", (ICustomerRepository customers) =>
                        {
                            return Results.Ok(customers.GetAll());
                        })
                            .Produces(StatusCodes.Status200OK)
                            .WithName("GetAllCustomers");

                        return app;


                    }))
            };
        }
    }
}
