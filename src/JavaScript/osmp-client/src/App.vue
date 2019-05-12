<template>
  <div id="app" style="height: 100vh;">
      <h1>OSMP Console</h1>
    <table style="width: 100%; height: 80%;">
        <tr style="height: 20px; width: 100%;">
            <td>
                <table>
                    <tr>
                        <td width="100%">
                            <input id="addressfield" type="text" style="width: 100%; height: 100%;" v-model="url" @keydown.enter="connect"/>
                        </td>
                        <td width="auto">
                            <input type="button" value="Connect" @click="connect"/>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        <tr style="width: 100%; height: 100%; background: whitesmoke;">
            <td style="width: 100%; height: 100%;">
                <div style="overflow-y: scroll; width: 100%; height: 100%; text-align: left;">
                    <pre v-for="text in textstream">{{text}}<br></pre>
                </div>
            </td>
        </tr>
        <tr style="height: 20px; width: 100%;">
            <td>
                <table>
                    <tr>
                        <td width="100%">
                            <input id="textfield" type="text" style="width: 100%; height: 100%;" v-model="cmd" placeholder="Type OSMP command and hit Enter" @keydown.enter="send_command"/>
                        </td>
                        <td width="auto">
                            <input type="button" value="send" @click="send_command"/>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
  </div>
</template>

<script>
import OSMP from "@/osmp-client";

export default {
  name: 'app',
  components: {  },
  data() {
      return {
          url:"ws://127.0.0.1:4433/osmp/v1",
          textstream:[],
          cmd:"",
      }
  },
    mounted() {
        if (this.url)
             this.connect();
        OSMP.event_received=(event, data) => this.textstream.push(">>> GOT EVENT: " + event + " " + JSON.stringify(data));
    },
    methods: {
        send_command() {
          console.log("sending OSMP command: " + this.cmd);
          this.textstream.push("> " + this.cmd)
          OSMP.send(this.cmd).then((data) => {
              this.textstream.push("   " + JSON.stringify(data));
          });
          this.cmd="";
        },
        connect(){
            OSMP.connect(this.url);
        }
    },
}
</script>

<style>
#app {
  font-family: 'Avenir', Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
}
    pre {
        white-space: pre-wrap;       /* css-3 */
        white-space: -moz-pre-wrap;  /* Mozilla, since 1999 */
        //white-space: -pre-wrap;      /* Opera 4-6 */
        white-space: -o-pre-wrap;    /* Opera 7 */
        word-wrap: break-word;       /* Internet Explorer 5.5+ */
    }
</style>
