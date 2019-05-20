# Speckle Revit

[![Build status](https://ci.appveyor.com/api/projects/status/03r7l9i2ohqqra2c?svg=true)](https://ci.appveyor.com/project/SpeckleWorks/specklerevitreboot)

The dream we've all been waiting for. 

![brevit](https://user-images.githubusercontent.com/7696515/58039037-7fd43500-7b29-11e9-9d72-f09733ede653.PNG)


## Current Project State:

- Can send stuff out of revit.
- Can receive some stuff in revit, albeit the gh authoring side is not yet released.

> TODO: Add some gifs/imgs.

## Project Structure

- `SpeckleRevit` - the main beast containing the interaction logic with Revit; implements and expands on the `SpeckleUiBindings` (see below). 
- `SpeckleUiBase` - submodule, the base ui. a cefsharp based app. see notes [here](https://github.com/speckleworks/SpeckleUi).
- `SpeckleCore` - the one and only .NET SDK for Speckle. 
- `SpeckleElements` - a rather basic object model (a speckle kit!) to faciliate getting data in and out of Revit. This is not a hard dependency, but in order for things to work you need something. 

## Setting up for development & debugging from scratch

These instructions might be incomplete. By all means, do submit a PR if something is wrong. 

**Step 1:** Clone this repository, with its submodules. The solution file you're looking for is *SpeckleRevitWithElements*.

**Step 2:** Fix broken project references. Most likely, you will need to initialise the sumbodules in `SpeckleElements` separately.

**Step 2a**: Build SpeckleElements. If you haven't debugged or developed for speckle before, this probably means that you need to set up and build for debug [SpeckleCoreGeometry](https://github.com/speckleworks/SpeckleCoreGeometry) too. At the end of the day, what you need to have is a folder in `%localappdata%` called `SpeckleKitsDebug` that contains:
- SpeckleElements
- SpeckleCoreGeometry

If you have installed speckle before, you can get away without building SpeckleCoreGeometry by just copy pasting it from the `%localappdata%/SpeckleKits` folder in the `%localappdata%/SpeckleKitsDebug` folder.

**Step 3:** Start a development server for the [ui app](https://github.com/speckleworks/SpeckleUi). 

If it's the first time you're doing this, this means running first `npm install` and then `npm run serve` (obviously, you will need node and npm installed first, as well as python). If things workded out fine, you'll be able to see something in your browser at `localhost:8000`. 

**Step 4:** Start debugging! This should launch Revit 2019 after a successful build. If you see the speckle plugin and its folder in `C:\Users\[your name]\AppData\Roaming\Autodesk\Revit\Addins\2019` you've probably nailed it. 

> I've tested the plugin with Revit 2018 too, and it should load fine in there. So if no Revit 2019, don't hesitate to change the path to your executable in the project debug settings. 


## Architecture, Dev Notes, etc: 

### Local State & Clients Serialisation

Every time a stream is baked, this is reflected and stored in a local state that is serialised within the revit document itself. Same goes for clients. 

The local state is injected in any kits that can accept it (and work with it - ideally all). 

### CEFSharp version

Dynamo and/or Revit 2019 ships with cefsharp too, hence we need to maintain feature parity with its version (57). Do not use newer versions of cefsharp!

### UIBindings class 

SpeckleUiBase exposes an abstract class `SpeckleUiBindings` that should be implemented in all the custom applications where this ui will be present. It's still in flux. It provides a couple default wrappers on common stuff:

- GetAccounts
- ShowDev
- NotifyUi - essentially emits an event to the ui. These are captured and handled [here](https://github.com/speckleworks/SpeckleUi/blob/master/App/src/main.js#L20-L30).

It also forces the implementation of some the basic functionality:
- get application host name
- add/remove sender, receiver - should persist the added clients in the host file
- bake receiver - plop the objects in the host app open file in one way or the other
- etc. 

This class is still in flux. If proposing changes/additions/ammendments to it, please bear in mind that it should expose functionality that is not tied to Revit only, but have a more generic nature.

**The SpeckleUiBindings class mentioned above IS also the CefSharp bound object in the browser**, so basically the web app can call on methods from the UiBindings object. For example: 

```js
let res = await UiBindings.getApplicationHostName( )
context.commit( 'SET_HOST_APP', res )
```
Another example:

```js
let res = await UiBindings.getAccounts( )
let accounts = JSON.parse( res )
```

### Selecting things for sending

I am increasingly of the opinion that users should manually select objects from the host application that they want to send, and use the host application's filtering mehtods to refine the selection to match their intent.

Previous thoughts on this matter include:

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

### The UI App:

- subscribe to events, and show expired/not expired state
- expose simple buttons to "bake" (receivers), 
- "push", and add/remove (see fliters above) objects from a sender

The ui ap is now in the `App` folder; should `npm install` and then `npm run serve`. It's a vue2.xx app, set up with the vue cli. For building, we'll need to set up other stuff laters.

~Extra notes: the app url is hard coded to `Browser.Address = @"http://10.211.55.2:8080/";` which should obvs be changed to something more reasonable on whatever env you're on.~

### The Revit External Event Handler

We make sure the bindings are aware of the event handler and vice-versa. There's an `Action` queue in the bindings that should store all needed actions (interactions with Revit). See "bake receiver" functionality.

The external event handler just iterates through that action queue and executes them all (sequentially) untill the list is empty. 

Not sure if this is the best way to do it, but so far it works. It can probably be optimised quite a bit. 

### SpeckleElements

Wow, you've read so far! Congrats. The idea behind speckle kits, and by extension SpeckleElements, deserves a blogpost on its own. Suffice to say a kit is a bunch of class defintions (that should inherit from SpeckleCore's SpeckleObject) and several separate projects that define the conversion logic for each application you want that specific kit to be relevant in. Even shorter, `kit = object definitions + conversion logic`. 

SpeckleElements is a simplistic take on an object model that would allow data to go in and out of revit, as well as be instantiable in Grasshopper easily. So far it contains: 
- walls
- beams
- columns
- levels
- grids
- shafts
- topography
- floor
- room 

Any of the revit elements that are not defined above get exported as either:
- FamilyInstance
- GenericElement 

Most of the elements inherit from SpeckleCoreGeometry/SpeckleMesh; this facilitates data egress from Revit and in the speckle liberal ecosystem. Some examples:
- MEP: https://hestia.speckle.works/#/view/6n01v82yc
- Arch: https://hestia.speckle.works/#/view/D4X2GfF2f
- Structure: https://hestia.speckle.works/#/view/uKz_QrTot


## Contributions

Contributions are welcome, albeit I do realise the rather complex project structure might thwart them. So, by all means, I am more than welcoming code reviews & simplifications. 

Ideally, the low hanging fruit are in the SpeckleElements project, where you can define conversion logic to and from Revit. 
