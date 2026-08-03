resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
    name: 'asp-staypilot-dev'
    location: 'northeurope'
    sku: { name: 'F1' }   // the tier/size
    properties: { reserved: true }   // the resource-spesific settings
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
    name: 'app-staypilot-dev'
    location: 'northeurope'
    properties: {
        serverFarmId: appServicePlan.id
        siteConfig: {
            linuxFxVersion: 'DOTNETCORE|10.0'
        }
    }
}
