import '../../Microsoft.JSInterop/JavaScriptRuntime/src/Microsoft.JSInterop';
import './GlobalExports';
import * as Environment from './Environment';
import * as signalR from '@aspnet/signalr';
import { MessagePackHubProtocol } from '@aspnet/signalr-protocol-msgpack';
import { OutOfProcessRenderBatch } from './Rendering/RenderBatch/OutOfProcessRenderBatch';
import { internalFunctions as uriHelperFunctions } from './Services/UriHelper';
import { renderBatch } from './Rendering/Renderer';

function boot() {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/_blazor')
    .withHubProtocol(new MessagePackHubProtocol())
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connection.on('JS.BeginInvokeJS', DotNet.jsCallDispatcher.beginInvokeJSFromDotNet);
  connection.on('JS.RenderBatch', (browserRendererId: number, batchData: Uint8Array) => {
    renderBatch(browserRendererId, new OutOfProcessRenderBatch(batchData));
  });

  connection.start()
    .then(() => {
      DotNet.attachDispatcher({
        beginInvokeDotNetFromJS: (callId, assemblyName, methodIdentifier, argsJson) => {
          connection.send('BeginInvokeDotNetFromJS', callId ? callId.toString() : null, assemblyName, methodIdentifier, argsJson);
        }
      });

      connection.send(
        'StartCircuit',
        uriHelperFunctions.getLocationHref(),
        uriHelperFunctions.getBaseURI()
      );
    })
    .catch(err => console.error(err));
}

boot();
