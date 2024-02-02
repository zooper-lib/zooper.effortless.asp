﻿using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zooper.Effortless.ASP.Communication.Refit.DelegatingHandler;
using Zooper.Effortless.ASP.Communication.Refit.Interfaces;
using Zooper.Effortless.ASP.Communication.Refit.Stores;

namespace Zooper.Effortless.ASP.Communication.Refit.Extensions;

public static class RefitExtensions
{
	public static IServiceCollection RegisterRefitRepositories(
		this IServiceCollection services,
		Assembly assembly)
	{
		foreach (var type in assembly.GetTypes())
		{
			if (!typeof(IRefitApi).IsAssignableFrom(type) || !type.IsClass) continue;

			foreach (var repositoryInterface in type.GetInterfaces())
				services.AddTransient(
					repositoryInterface,
					type
				);
		}

		return services;
	}

	public static IServiceCollection RegisterRefitAutoRefreshHttpHandler(
		this IServiceCollection services,
		string clientId,
		string clientSecret,
		Uri tokenEndpoint)
	{
		services.AddSingleton<IAccessDataStore, InMemoryAccessDataStore>();

		services.AddTransient<AutoRefreshTokenDelegatingHandler>(
			sp =>
			{
				var accessDataStore = sp.GetRequiredService<IAccessDataStore>();
				var logger = sp.GetRequiredService<ILogger<AutoRefreshTokenDelegatingHandler>>();

				return new(
					accessDataStore,
					logger,
					clientId,
					clientSecret,
					tokenEndpoint
				);
			}
		);

		return services;
	}
}