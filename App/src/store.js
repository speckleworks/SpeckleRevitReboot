import Vue from 'vue'
import Vuex from 'vuex'
import Axios from 'axios'

Vue.use( Vuex )

export default new Vuex.Store( {
  state: {
    accounts: [ ],
    clients: [ ],
    hostAppName: null
  },
  mutations: {
    ADD_CLIENT( state, client ) {
      state.clients.unshift( client )
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
    addReceiverClient: ( context, account, stream ) => new Promise( async ( resolve, reject ) => {
      await UiBindings.addReceiver( )
      let client = { ...stream }
      client.AccountId = account.AccountId
      client.type = 'receiver'
      context.commit( 'ADD_CLIENT', client )
    } ),

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
          context.commit( 'SET_ACCOUNT_DATA', { ...account, validated: true, streams: res.data.resources } )
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
  }
} )