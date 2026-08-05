resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
    name: 'asp-staypilot-dev'
    location: 'swedencentral'
    sku: { name: 'F1' }   // the tier/size
    properties: { reserved: true }   // the resource-spesific settings
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
    name: 'app-staypilot-dev'
    location: 'swedencentral'
    properties: {
        serverFarmId: appServicePlan.id
        siteConfig: {
            linuxFxVersion: 'DOTNETCORE|10.0'
            appSettings: [
                {
                    name: 'ConnectionStrings__DefaultConnection'
                    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=db-staypilot-dev;User ID=dmytrolenivenko;Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;'
                }
            ]
            cors: {
                allowedOrigins: [
                    'https://${staticWebApp.properties.defaultHostname}'
                ]
            }
        }
    }
}

@secure()
param sqlAdminPassword string

resource sqlServer 'Microsoft.Sql/servers@2025-02-01-preview' = {
    name: 'srv-staypilot-dev'
    location: 'swedencentral'
    properties: {
        administratorLogin: 'dmytrolenivenko'
        administratorLoginPassword: sqlAdminPassword
    }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2025-02-01-preview' = {
    name: 'db-staypilot-dev'
    location: 'swedencentral'
    parent: sqlServer
    sku: {
        name: 'GP_S_Gen5_2'
        tier: 'GeneralPurpose'
        family: 'Gen5'
        capacity: 2
    }
    properties: {
        autoPauseDelay: 60
        minCapacity: json('0.5')
        useFreeLimit: true
        freeLimitExhaustionBehavior: 'AutoPause'
    }    
}

resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2025-02-01-preview' = {
    name: 'AllowAllAzureIps'
    parent: sqlServer
    properties: {
        startIpAddress: '0.0.0.0'
        endIpAddress: '0.0.0.0'
    }
}

resource staticWebApp 'Microsoft.Web/staticSites@2025-03-01' = {
    name: 'stapp-staypilot-dev'
    location: 'centralus'
    sku: {
        name: 'Free'
        tier: 'Free'
    }
    properties: {
        stagingEnvironmentPolicy: 'Enabled'
        allowConfigFileUpdates: true
        provider: 'None'
        enterpriseGradeCdnStatus: 'Disabled'
        deploymentAuthPolicy: 'DeploymentToken'
    }
}
