function clientCmdSendXPMSupportHandshake() {
	commandToServer('onReceiveXPMSupportHandshake');
}

function clientCmdReceiveXPMSaveRequest(%filename) {
	$CXPM::LocalFile = new FileObject();
	$CXPM::LocalFile.openForWrite(%filename);

	$CXPM::MultiLine = "";
	commandToServer('onReceiveSaveRequestAccepted');
}
function clientCmdReceiveXPMLine(%data) {
	$CXPM::LocalFile.writeLine(%data);
}
function clientCmdReceiveXPMMultiLine(%data) {
	$CXPM::MultiLine = $CXPM::MultiLine @ %data;
}
function clientCmdReceiveXPMMultiLineFinish() {
	$CXPM::LocalFile.writeLine($CXPM::MultiLine);
	$CXPM::MultiLine = "";
}
function clientCmdReceiveXPMSaveDoneRequest() {
	$CXPM::LocalFile.close();
	$CXPM::LocalFile.delete();
}