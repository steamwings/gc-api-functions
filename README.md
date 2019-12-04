## Branch: containerizable
This exists with the hope that one day we'll want to containerize our functions once again

## Functions Code
API function apps

Functions:
- ValidateAddress

### FunctionTests
Includes TestHelper class and test cases for each function.

#### ValidateAddress
2 positive
1 negative

## Docker containers
TODO: Generate images a webhook task in the Azure container registry?

azure-pipelines.yml is associated with paused pipeline in Azure DevOps

### Test locally
Build in debug mode with
`docker build -t gcapi<name>:<version> -f Dockerfile-dbg`

