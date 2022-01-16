using System;
using Microsoft.Extensions.DependencyInjection;

static class IServiceProviderExtensions
{
	public static T CreateInstance<T>(this IServiceProvider serviceProvider, params object[] args)
		=> ActivatorUtilities.CreateInstance<T>(serviceProvider, args);
}
