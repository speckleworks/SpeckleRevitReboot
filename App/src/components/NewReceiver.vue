<template>
  <v-card>
    <v-overflow-btn :items="$store.state.accounts" label="Account" editable solo v-model='selectedAccount' item-text='fullName' return-object xxxxprepend-icon='account_circle' xxxstyle='margin-left:10px'></v-overflow-btn>
    <!-- <v-tabs fixed-tabs color='gray' style='top:-28px'> -->
    <!--       <v-tab key="Receiver">
        Receiver
      </v-tab> -->
    <!-- <v-tab-item key='Receiver'> -->
    <!-- <br> -->
    <v-card-text>
      <v-layout row v-if='selectedAccount && selectedAccount.streams.length > 0'>
        <v-flex xs11>
          <v-overflow-btn :items="selectedAccount.streams" label="Select from your streams" editable v-model='selectedStream' item-text='fullName' return-object solo xxxprepend-icon='cloud_download'></v-overflow-btn>
        </v-flex>
        <v-flex text-sm-center>
          <v-btn icon @click.native='refreshStreamsAtAccount'>
            <v-icon>refresh</v-icon>
          </v-btn>
        </v-flex>
      </v-layout>
      <v-layout>
        {{selectedStream}}
      </v-layout>
    </v-card-text>
    <v-card-text v-if='!selectedAccount || !selectedAccount.validated'>
      Could not access that server - are you and the server online?
    </v-card-text>
    <v-card-actions>
      <v-spacer></v-spacer>
      <v-btn color="secondary" flat @click='$emit("close")' :ripple="false">
        cancel
      </v-btn>
      <v-btn color="primary" flat :ripple="false" :disabled='selectedStream===null' @click.native='addReceiver()'>
        GO!
      </v-btn>
    </v-card-actions>
  </v-card>
</template>
<script>
export default {
  name: 'NewClient',
  props: {
    isVisible: { type: Boolean, default: false }
  },
  watch: {
    isVisible( val ) {
      if ( val ) {
        this.selectedAccount = this.$store.state.accounts.find( ac => ac.IsDefault === true )
        this.selectedStream = null
      }
    }
  },
  data: ( ) => ( {
    selectedAccount: null,
    selectedStream: null,
  } ),
  methods: {
    refreshStreamsAtAccount( ) {
      this.$store.dispatch( 'getAccountStreams', this.selectedAccount )
    },
    async addReceiver( ) {
      let res = await this.$store.dispatch( 'addReceiverClient', { account: this.selectedAccount, stream: this.selectedStream } )
      this.$emit( "close" )
    }
  }
}
</script>
<style scoped lang='scss'>
</style>