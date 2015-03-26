function clientCmdSendXPMSupportHandshake() {
	commandToServer('onReceiveXPMSupportHandshake');
}

function clientCmdReceiveXPMSaveRequest(%filename) {
	$CXPM::LocalFile = new FileObject();
	$CXPM::LocalFile.openForWrite(%filename);

	commandToServer('onReceiveSaveRequestAccepted');
}
function clientCmdReceiveXPMLine(%data) {
	$CXPM::LocalFile.writeLine(%data);
}
function clientCmdReceiveXPMSaveDoneRequest() {
	$CXPM::LocalFile.close();
	$CXPM::LocalFile.delete();
}