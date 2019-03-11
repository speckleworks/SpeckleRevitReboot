<template>
  <v-container grid-list-md mb-4 pa-0 v-if='client' class='elevation-1'>
    <v-layout align-center>
      <v-flex xs2>
        <v-btn fab :outline='!client.expired' icon color='primary' @click.native='bakeReceiver()' :loading='client.loading'>
          <v-icon>{{ client.expired?"cloud_download":"check_circle"}}</v-icon>
        </v-btn>
      </v-flex>
      <v-flex text-xs-left>
        <span class='title'>{{client.name}}</span>
        <br>
        <span class='caption'>
          {{account.ServerName}}
        </span>
        <!-- <v-divider></v-divider> -->
      </v-flex>
      <v-flex text-xs-right>
        <v-chip outline color='primary'>
          <span class='caption' style="user-select:all">
            {{client.streamId}}
          </span>
        </v-chip>
      </v-flex>
    </v-layout>
    <v-layout row wrap align-center>
      <v-flex xs12>
        <v-divider></v-divider>
      </v-flex>
      <v-flex xs10 text-xs-left pl-4 class='caption'>
        Last update: {{updatedAt}} (<b>
          <timeago :datetime="client.updatedAt" :auto-update="60"></timeago>
        </b>) | {{client.expired}}
      </v-flex>
      <v-flex xs2 text-xs-right>
        <v-btn small flat icon color='error' @click.native='deleteClient'><v-icon>delete</v-icon></v-btn>
        <!-- <v-btn small flat color='primary'>admin</v-btn> -->
      </v-flex>
      <v-flex xs12>
        <!-- <v-divider></v-divider> -->
      </v-flex>
    </v-layout>
  </v-container>
</template>
<script>
import Sockette from 'sockette'

export default {
  name: "Client",
  props: {
    client: {
      type: Object,
      default: null
    }
  },
  computed: {
    account( ) {
      return this.$store.state.accounts.find( ac => ac.AccountId === this.client.AccountId )
    },
    updatedAt( ) {
      return ( new Date( this.client.updatedAt ) ).toLocaleDateString( )
    }
  },
  data: ( ) => ( {
    timeAgoUpdate: null
  } ),
  methods: {
    bakeReceiver( ) {
      this.$store.dispatch( 'bakeReceiver', this.client )
    },
    deleteClient( ) {
      this.$store.dispatch( 'removeReceiverClient', this.client )
      this.sockette.close( )
    },
    wsOpen( e ) {
      this.sockette.json( { eventName: 'join', resourceType: 'stream', resourceId: this.client.streamId } )
    },
    wsMessage( e ) {
      console.log( e.data )
      if ( e.data === 'ping' ) {
        this.sockette.send( 'alive' )
        return
      }
      try {
        let message = JSON.parse( e.data )
        switch ( message.args.eventType ) {
          case 'update-global':
            this.$store.dispatch( 'updateClient', { client: this.client, expire: true } )
            break
          case 'update-meta':
            this.$store.dispatch( 'updateClient', { client: this.client, expire: false } )
            break
        }

      } catch ( err ) {
        console.warn( `Could not parse/interpret ${e.data} for ${this.client.streamId}` )
        console.log( e.data )
      }

    },
    wsError( e ) { console.log( e ) },
    wsReconnect( e ) { console.log( e ) },
    wsClose( e ) { console.log( e ) }
  },
  mounted( ) {
    console.log( 'client mounted!' )
    console.log( this.client )
    let wsUrl = this.account.RestApi.replace( 'http', 'ws' )
    this.sockette = new Sockette( `${wsUrl}?client_id=${this.client.clientId}&access_token=${this.account.Token}`, {
      timeout: 5e3,
      maxAttempts: 100,
      onopen: this.wsOpen,
      onmessage: this.wsMessage,
      onerror: this.wsError,
      onreconnect: this.wsReconnect,
      onclose: this.wsClose,
    } )
  },
  beforeDestroy( ) {
    console.log( 'bye bye...' )
    this.sockette.close( )
  }
}
</script>
<style scoped lang='scss'>
</style>