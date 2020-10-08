module Config


open Microsoft.Extensions.Configuration;

[<CLIMutable>]
type Comdirect_Api =
  {
    Client_Id : string
    Client_Secret : string
  }

[<CLIMutable>]
type Config = {
  Comdirect_Api: Comdirect_Api 
}

let fetch () =
  let builder = (new ConfigurationBuilder())
                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, true)
                    .AddEnvironmentVariables(); 
  let configurationRoot = builder.Build();
  configurationRoot.Get<Config>()  