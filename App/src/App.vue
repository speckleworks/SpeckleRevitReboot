<template>
  <v-app>
    <v-toolbar app>
      <v-toolbar-title class="headline text-uppercase">
        <span @click='showDev()'>Speckle </span>
        <span class="font-weight-light">{{$store.state.hostAppName}}</span>
      </v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn color="primary" dark small absolute bottom right fab :ripple="false" @click.native='showAddNew=true'>
        <v-icon>{{showAddNew ? "close" : "add"}}</v-icon>
      </v-btn>
    </v-toolbar>
    <v-dialog v-model="showAddNew" scrollable xxxmax-width="300px" hide-overlay>
      <NewReceiver :is-visible='showAddNew' @close='showAddNew=false'>
      </NewReceiver>
    </v-dialog>
      <v-content>
        <v-container grid-list-md text-xs-center>
          <v-layout row wrap>
            <v-flex xs12>
              // TODO: Add clients
            </v-flex>
          </v-layout>
        </v-container>
      </v-content>
  </v-app>
</template>
<script>
import HelloWorld from './components/HelloWorld'
import NewReceiver from './components/NewReceiver.vue'

export default {
  name: 'App',
  components: {
    HelloWorld,
    NewReceiver
  },
  data( ) {
    return {
      showAddNew: false
    }
  },
  methods: {
    showDev( ) {
      console.log( 'showing dev' )
      UiBindings.showDev( )
    }
  },
  mounted( ) {
    console.log( 'app mounted!' )

    this.$store.dispatch( 'getAccounts' )
    this.$store.dispatch( 'getApplicationHostName' )
  }
}
</script>