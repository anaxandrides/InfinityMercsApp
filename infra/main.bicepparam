using 'main.bicep'

param location = 'eastus'
param environmentName = 'prod'

// The Entra External ID tenant (infinitymercs.onmicrosoft.com) and the API's app registration (docs/azure-setup.md).
param authAuthority = 'https://infinitymercs.ciamlogin.com/36af320a-cd91-43b1-8c24-f1c21c798455/v2.0'
param authAudience = '6d09c3c6-41d1-4c53-a256-d65e8e4af405'
