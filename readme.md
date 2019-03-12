## Structure

SpeckleCore - submodule of Speckle .net core sdk
SpeckleRevitReboot - the main beast containing revit logics
SpeckleRevitUiBase - submodule, the base ui. a cefsharp based app. see notes here: https://github.com/didimitrie/SpeckleUi

Note: this repo should have nothing to do with an object model (object definitions). Those should be grouped under a speckle kit.



## Generic dev notes:

Dynamo and/or Revit 2019 ships with cefsharp too, hence we need to maintain feature parity with its version (57). Do not use newer versions of cefsharp!

SpeckleUiBase exposes an abstract class `SpeckleUiBindings` that should be implemented in all the custom applications where this ui will be present. It provides a couple default wrappers on common stuff:

- GetAccounts
- ShowDev
- NotifyUi - essentially emits an event in the ui

and forces the implementation of some the basic functionality:
- get application host name
- add/remove sender, receiver - should persist the added clients in the host file
- bake receiver - plop the objects in the host app open file in one way or the other
- etc. 

Questions will come on how we allow the sender to select what to send. This will be a bit more difficult to generalise. Some options: 

- by selection - simple stuff
- by pre-made filters to be sent to the ui as a base - ie, get a list of filters that the user can select what to add

```js
let filter = {
  filterName: "Some Name" // ie, Revit Family name or layer name or whatever
  filterType: "Rhino Layer" // can also be something else, ie, Revit Family
  numberOfObjects: 42
}
```

Then this gets sent back to the implemented `SoftwareXXXSpeckleUiBindings` which knows how to deal with it thereafter. 

The ui app responsibilities: 
- subscribe to events, and show expired/not expired state
- expose simple buttons to "bake" (receivers), 
- "push", and add/remove (see fliters above) objects from a sender

The ui ap is now in the `App` folder; should `npm install` and then `npm run serve`. It's a vue2.xx app, set up with the vue cli. For building, we'll need to set up other stuff laters.

**The SpeckleUiBindings class mentioned above IS also the CefSharp bound object in the browser**, so basically the App does stuff like 

```js
let res = await UiBindings.getApplicationHostName( )
context.commit( 'SET_HOST_APP', res )
```
or

```js
let res = await UiBindings.getAccounts( )
let accounts = JSON.parse( res )
```

Let's try and keep those as much as possible in the store...

Extra notes: the app url is hard coded to `Browser.Address = @"http://10.211.55.2:8080/";` which should obvs be changed to something more reasonable on whatever env you're on.
