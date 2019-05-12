/*
 * You may use Open System Management Protocol and its Reference Implementation free of charge as long as you honor
 * the protocol specification. You may not use, license, distribute or advertise the protocol or any derivations of
 * it under a different name.

 * The Open System Management Protocol is Copyright © by Eqqon GmbH

 * THE PROTOCOL AND ITS REFERENCE IMPLEMENTATION ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
 * ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE PROTOCOL OR ITS REFERENCE
 * IMPLEMENTATION OR THE USE OR OTHER DEALINGS IN THE PROTOCOL OR REFERENCE IMPLEMENTATION.
 */

export default {
    // port: 4433,
    // host: "localhost",
    // version: 1,
    ws:null,
    msg_count:0,
    _waiting_cmds: {},
    _event_handlers: {},
    event_received(eventname, data) { //hook in your global event handler here by replacing this function with your handler
        console.log("got event:", eventname, data);
    },
    connect(url) {
        try {
            console.log("opening connection to: "+url);
            this.ws=new WebSocket(url);
            var promiseResolve, promiseReject;
            var promise = new Promise(function(resolve, reject){
                promiseResolve = resolve;
                promiseReject = reject;
            });
            this.ws.onopen = event => {
                console.log("connection is open.");
                promiseResolve();
            };
            this.ws.onmessage = event => {
                console.log("ws got data:", event.data);
                var msg=JSON.parse(event.data);
                switch (msg.type) {
                    case 'cmd':
                        break;
                    case 'event':
                        try {
                            if (msg.data.events) {
                                msg.data.events.forEach(ev=> {
                                    if (this.event_received)
                                        this.event_received(ev.event, ev.data);
                                    if (this._event_handlers.hasOwnProperty(ev.event)) {
                                        var handler=this._event_handlers[ev.event];
                                        handler.func(ev.data);
                                    }
                                });
                            }
                            else {
                                if (this.event_received)
                                    this.event_received(msg.id, msg.data);
                                if (this._event_handlers.hasOwnProperty(msg.id)) {
                                    var handler = this._event_handlers[msg.id];
                                    handler.func(msg.data);
                                }
                            }
                        }
                        catch(e) {
                            console.error(e);
                        }
                        break;
                    case 'response':
                        var cmdnr=msg['cmd-nr'];
                        if (!this._waiting_cmds.hasOwnProperty(cmdnr)) {
                            console.error("response " + cmdnr + " does not match any waiting command nr");
                            break;
                        }
                        var waiting_cmd=this._waiting_cmds[cmdnr];
                        delete this._waiting_cmds[cmdnr];
                        waiting_cmd.resolve(msg);
                        break;
                }
            };
        }
        catch(e) {
            console.error(e);
        }
        return promise;
    },
    close() {
        if (!this.ws)
            return;
        console.log("closing connection");
        try {
            if (this.ws.readyState<2)
                this.ws.close();
        }
        catch(e) {
            console.error(e);
        }
    },
    send(cmd, data) {
        if(!this.ws) {
            console.error("OSMP client Can not send! Not connected?");
            return;
        }
        this.msg_count += 1;
        var nr=this.msg_count;
        // var cmd = cmdline.split(" ", 1)[0];
        // var data=null;
        // try {
        //     data = JSON.parse(cmdline.substr(cmd.length));
        // }
        // catch {}
        var payload = {'nr': nr, 'type': 'cmd', 'id': cmd,}
        if (data) {
            payload.data = data;
        }
        var promiseResolve, promiseReject;
        var promise = new Promise(function(resolve, reject){
            promiseResolve = resolve;
            promiseReject = reject;
        });
        this._waiting_cmds[nr]={'cmd':cmd, 'nr':nr, 'resolve':promiseResolve, 'reject':promiseReject };
        var msgdata=JSON.stringify(payload);
        console.log("sending: " + msgdata);
        this.ws.send(msgdata);
        return promise;
    },
    event_subscribe(name, func) {
        this._event_handlers[name]={func:func, subscribed_date:Date.now() };
        this.send("event-subscribe", {event:name});
    },
    event_unsubscribe(name) {
        delete this._event_handlers[name];
        this.send("event-unsubscribe", {event:name});
    },
}