# Function App API

## Functions
- Login (POST)*
- Register (POST)*
- ValidateAddress (POST)
- Profile (GET)
- Profile (PUT)

\* These functions do not require a JWT token for authorization.

All functions require a function key (or API key if accessing via the API gateway).

## Projects Overview
- **Functions**: the functions being deployed
- **FunctionsTests**: comprehensive unit and integration tests
- **Models**: data models
    - **Database**: models used only by the backend db
    - **UI**: models used only by the frontend
    - **Common**: models used by UI and database 

## Design Notes

### Model Transforms
UI models will need to be created by transforms (e.g. flattening or mapping) of Common and Database models. We do not have any transforms yet, but it's desired that transforms:
- Are flexible and reusable (and thus built in a generic form)
- Are NOT just code that copies properties manually from other models
- Are highly performant
- Do not have any complex operations
- Do not have queries

### Multiple Function Apps
Currently, we have all our functions in one function app. At some point, it will likely be more efficient to split them into more and less frequently, so that they can scale separately.

### Docker containers
...can be used for Azure functions, but not on the consumption tier. **When we move to the premium tier, we should switch to Docker containers.** 

The containers can generated by a webhook task in an Azure container registry. The webhook can be linked to an Azure DevOps build pipeline.

#### Test locally
Build containers in debug mode with
`docker build -t gcapi<name>:<version> -f Dockerfile-dbg`

