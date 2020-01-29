# You may use the Open System Management Protocol and its reference implementations free of charge for commercial and non-commercial
# purposes as long as you honor the protocol specification. You may not use, license,  distribute or advertise the protocol under a
# different name and you must retain the following copyright notice.
#
# Open System Management Protocol is Copyright © by Eqqon GmbH
#
# THE PROTOCOL AND ITS REFERENCE IMPLEMENTATION ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
# NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
# OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
# FROM, OUT OF OR IN CONNECTION WITH THE PROTOCOL OR ITS REFERENCE IMPLEMENTATION OR THE USE OR OTHER DEALINGS IN THE PROTOCOL OR
# REFERENCE IMPLEMENTATION.

class OsmpServer(object):
    def __init__(self, uri="/osmp/v1", port=443, use_ssl=True):
        self.uri=uri
        self.port=port
        self.use_ssl=use_ssl
        # todo: transfer stats
        # todo: command modules
        self.enabled_instruction_sets=["standard"]
        self.commands={}
        self.events={}
        self.global_env={}
