﻿using Microsoft.Extensions.Configuration;

namespace Zooper.Effortless.ASP.Secrets.HashicorpVault;

public class HashicorpVaultConfigurationSource(
	string vaultUrl,
	string token,
	string mountPoint) : IConfigurationSource
{
	public string VaultUrl { get; private set; } = vaultUrl;
	public string Token { get; private set; } = token;
	public string MountPoint { get; private set; } = mountPoint;

	public IConfigurationProvider Build(IConfigurationBuilder builder)
	{
		return new HashicorpVaultConfigurationProvider(this);
	}
}