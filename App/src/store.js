import Vue from 'vue'
import Vuex from 'vuex'
import Axios from 'axios'

Vue.use( Vuex )

export default new Vuex.Store( {
  state: {
    accounts: [ ],
    clients: [ ],
    hostAppName: null,
    errors: [ ]
  },
  mutations: {
    ADD_CLIENT( state, client ) {
      state.clients.unshift( client )
    },

    REMOVE_CLIENT( state, _id ) {
      let index = state.clients.findIndex( cl => cl._id === _id )
      if ( index >= 0 )
        state.clients.splice( index, 1 )
      else
        console.error( 'client not found', _id )
    },

    SET_CLIENT_DATA( state, props ) {
      let found = state.clients.find( cl => cl._id === props._id )
      Object.keys( props ).forEach( key => {
        found[ key ] = props[ key ]
      } )
    },

    SET_ACCOUNTS( state, accounts ) {
      state.accounts = accounts
    },

    SET_ACCOUNT_DATA( state, props ) {
      let found = state.accounts.find( a => a.AccountId === props.AccountId )
      Object.keys( props ).forEach( key => {
        found[ key ] = props[ key ]
      } )
    },

    SET_HOST_APP( state, appName ) {
      state.hostAppName = appName
    }
  },
  actions: {
    bakeReceiver: ( context, client ) => new Promise( async ( resolve, reject ) => {
      await UiBindings.bakeReceiver( JSON.stringify( client ) )
      client.expired = false
      client.loading = false
      context.commit( 'SET_CLIENT_DATA', { _id: client._id, expired: false, loading: false } )
    } ),

    addReceiverClient: ( context, { account, stream } ) => new Promise( async ( resolve, reject ) => {
      let client = { ...stream }

      client.AccountId = account.AccountId
      client.account = { RestApi: account.RestApi, Email: account.Email, Token: account.Token }
      client.type = 'receiver'
      client.expired = true
      client.loading = false
      client.clientId = null
      let docName = await UiBindings.getFileName( )
      let docId = await UiBindings.getDocumentId( )
      let res = await Axios.post( `${account.RestApi}/clients`, { documentType: context.state.hostAppName, streamId: stream.streamId, documentName: docName, documentGuid: docId, role: 'receiver' }, { headers: { Authorization: account.Token } } )
      client.clientId = res.data.resource._id
      context.commit( 'ADD_CLIENT', client )

      let dupe = { ...client }
      dupe.account = { ...dupe.account }
      delete dupe.account.Token
      await UiBindings.addReceiver( JSON.stringify( client ) )
      return resolve( )
    } ),

    removeReceiverClient: ( context, client ) => new Promise( async ( resolve, reject ) => {
      await UiBindings.removeReceiver( JSON.stringify( client ) )
      await Axios.delete( `${client.account.RestApi}/clients/${client.clientId}`, { headers: { Authorization: client.account.Token } } )
      context.commit( 'REMOVE_CLIENT', client._id )
    } ),

    updateClient: ( context, { client, expire } ) => new Promise( async ( resolve, reject ) => {
      // note: real update, with all the heavy object lifting, happens in .NET
      let res = await Axios.get( `${client.account.RestApi}/streams/${client.streamId}?fields=name,updatedAt`, { headers: { Authorization: client.account.Token } } )
      console.log( res.data.resource )
      let cl = { _id: res.data.resource._id, name: res.data.resource.name, updatedAt: res.data.resource.updatedAt }
      console.log( expire )
      if ( expire ) cl.expired = true
      context.commit( 'SET_CLIENT_DATA', cl )
    } ),

    // updateClientMeta: ( context, client ) => new Promise( async ( resolve, reject ) => {
    //   let res = await Axios.get( `${client.account.RestApi}/streams?fields=name,updatedAt`, { headers: { Authorization: client.account.Token } } )
    // } ),

    getAccounts: ( context ) => new Promise( async ( resolve, reject ) => {
      let res = await UiBindings.getAccounts( )
      let accounts = JSON.parse( res )

      accounts.forEach( ac => {
        ac.fullName = ac.Email + ' - ' + ac.ServerName
        ac.streams = [ ]
        ac.validated = false
        context.dispatch( 'getAccountStreams', ac )
      } )

      context.commit( 'SET_ACCOUNTS', accounts )
    } ),

    getAccountStreams: ( context, account ) => new Promise( async ( resolve, reject ) => {
      Axios.get( `${account.RestApi}/streams?fields=streamId,name,updatedAt,parent&deleted=false&isComputedResult=false&sort=updatedAt`, { headers: { Authorization: account.Token } } )
        .then( res => {
          res.data.resources.forEach( s => s.fullName = `${s.streamId} - ${s.name}` )
          let sorted = res.data.resources.sort( ( a, b ) => {
            let ad = new Date( a.updatedAt )
            let bd = new Date( b.updatedAt )
            return ad > bd ? -1 : 1
          } ).filter( s => s.parent === null )
          context.commit( 'SET_ACCOUNT_DATA', { ...account, validated: true, streams: sorted } )
          resolve( res.data.resources )
        } )
        .catch( err => {
          // console.log( err )
          context.commit( 'SET_ACCOUNT_DATA', { ...account, validated: false } )
          reject( err )
        } )
    } ),

    getApplicationHostName: ( context ) => new Promise( async ( resolve, reject ) => {
      let res = await UiBindings.getApplicationHostName( )
      context.commit( 'SET_HOST_APP', res )
    } ),

    getExistingClients: ( context ) => new Promise( async ( resolve, reject ) => {
      let clients = JSON.parse( await UiBindings.getFileClients( ) )
      console.log( clients )
      if ( clients.length === 0 ) return resolve( )
      clients.forEach( existingClient => {
        let account = context.state.accounts.find( ac => ac.Email === existingClient.account.Email && ac.RestApi === existingClient.account.RestApi )
        if ( account !== null ) {
          existingClient.account.Token = account.Token
          context.commit( 'ADD_CLIENT', existingClient )
        } else {
          console.warn( 'no account found for client. sorrrrry!', existingClient )
        }
      } )
    } ),
  }
} )