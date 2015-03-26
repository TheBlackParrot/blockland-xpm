$GlobalXPM::Version = "v0.2-1";

// support functions
function RGBToHex(%rgb) {
	%rgb = getWords(%rgb,0,2);
	for(%i=0;%i<getWordCount(%rgb);%i++) {
		%dec = mFloor(getWord(%rgb,%i)*255);
		%str = "0123456789ABCDEF";
		%hex = "";

		while(%dec != 0) {
			%hexn = %dec % 16;
			%dec = mFloor(%dec / 16);
			%hex = getSubStr(%str,%hexn,1) @ %hex;    
		}

		if(strLen(%hex) == 1)
			%hex = "0" @ %hex;
		if(!strLen(%hex))
			%hex = "00";

		%hexstr = %hexstr @ %hex;
	}

	if(%hexstr $= "") {
		%hexstr = "FF00FF";
	}
	return %hexstr;
}

function Player::getLookingAt(%this,%distance)
{
	if(!%distance) {
		%distance = 100;
	}

	%eye = vectorScale(%this.getEyeVector(),%distance);
	%pos = %this.getEyePoint();
	%mask = $TypeMasks::FxBrickObjectType;
	%hit = firstWord(containerRaycast(%pos, vectorAdd(%pos, %eye), %mask, %this));
		
	if(!isObject(%hit)) {
		return;
	}
		
	if(%hit.getClassName() $= "fxDTSBrick") {
		return %hit;
	}
}


// XPM functions
function saveXPMFile(%client) {
	$XPM::Linecount = 3;
	$XPM::UsedColors = 0;
	$XPM::Line[0] = "! XPM2";
	$XPM::Line[2] = "zz c #000000";

	%box_center = getBoxCenter(%client.XPMStartPos SPC %client.XPMEndPos);
	%box_size = vectorSub(%client.XPMStartPos,%client.XPMEndPos);
	%bricks = 0;
	%highest_x = -9999999;
	%highest_y = -9999999;
	%lowest_x = 9999999;
	%lowest_y = 9999999;

	initContainerBoxSearch(%box_center,mAbs(getWord(%box_size,0)) SPC mAbs(getWord(%box_size,1)) SPC mAbs(getWord(%box_size,2)),$TypeMasks::FXBrickObjectType);
	while((%targetObject = containerSearchNext()) != 0 && isObject(%targetObject)) {
		$XPM::BrickSelection[%bricks] = %targetObject;
		%color_used = 0;
		if(%used_datablock != %targetObject.getDatablock() && %used_datablock !$= "") {
			talk("ERROR: BRICKS ARE NOT THE SAME DATABLOCK");
			return;
		}
		%targetObject.setName("_" @ getWord(%targetObject.getPosition(),0) @ "_" @ getWord(%targetObject.getPosition(),1) @ "_" @ getWord(%targetObject.getPosition(),2));
		%used_datablock = %targetObject.getDatablock();
		%static_z = getWord(%targetObject.getPosition(),2);

		%x_pos = getWord(%targetObject.getPosition(),0);
		%y_pos = getWord(%targetObject.getPosition(),1);
		if(%x_pos > %highest_x) {
			%highest_x = %x_pos;
		}
		if(%x_pos < %lowest_x) {
			%lowest_x = %x_pos;
		}
		if(%y_pos > %highest_y) {
			%highest_y = %y_pos;
		}
		if(%y_pos < %lowest_y) {
			%lowest_y = %y_pos;
		}

		for(%i=0;%i<$XPM::UsedColors;%i++) {
			if($XPM::Color[%i] == %targetObject.colorID) {
				%color_used = 1;
			}
		}
		if(!%color_used) {
			$XPM::Color[$XPM::UsedColors] = %targetObject.colorID;
			$XPM::UsedColors++;
		}

		%bricks++;
		//%targetObject.oldColor = %targetObject.colorID;
		//%targetObject.schedule(10,setColor,0);
		//%targetObject.schedule(10+(10*%i),setColor,%targetObject.oldColor);
	}

	%char_str = "abcdefghijklmnopqrstuvwxyz";
	for(%i=0;%i<$XPM::UsedColors;%i++) {
		%selected_str = getSubStr(%char_str,mFloor($XPM::Color[%i]/strLen(%char_str)),1) @ getSubStr(%char_str,$XPM::Color[%i] % strLen(%char_str),1);
		$XPM::Line[$XPM::Linecount] = %selected_str SPC "c" SPC "#" @ RGBToHex(getColorIDTable($XPM::Color[%i]));
		$XPM::Linecount++;
	}

	%increment_x = %used_datablock.brickSizeX/2;
	%increment_y = %used_datablock.brickSizeY/2;

	%found = 0;
	%attempts = 0;
	%width = 0;
	%height = 0;
	%loop_start = getRealTime();

	for(%x=%lowest_x;%x<=%highest_x;%x+=%increment_x) {
		%width = 0;
		for(%y=%lowest_y;%y<=%highest_y;%y+=%increment_y) {
			%brick = "_" @ %x @ "_" @ %y @ "_" @ %static_z;
			%attempts++;
			if(isObject(%brick)) {
				$XPM::Line[$XPM::Linecount] = $XPM::Line[$XPM::Linecount] @ getSubStr(%char_str,mFloor(%brick.colorID/strLen(%char_str)),1) @ getSubStr(%char_str,%brick.colorID % strLen(%char_str),1);
				%found++;
				%width++;
			} else {
				$XPM::Line[$XPM::Linecount] = $XPM::Line[$XPM::Linecount] @ "zz";
				%width++;
			}
			if(getRealTime() - %loop_start > 10000) {
				talk("EMERGENCY END");
				return;
			}
		}
		%height++;
		$XPM::Linecount++;
		if(getRealTime() - %loop_start > 10000) {
			talk("EMERGENCY END");
			return;
		}
	}

	echo("Used" SPC %found SPC "bricks. (attempted" SPC %attempts SPC "times)");

	$XPM::Line[1] = %width SPC %height SPC $XPM::UsedColors+1 SPC 2;
	endXPMSave(%client);
}

function serverCmdSaveXPMFile(%this,%filename) {
	if(%this.supportsXPM) {
		if(%this.XPMStartPos !$= "" && %this.XPMEndPos !$= "") {
			$XPM::StartTime = getRealTime();
			messageAll('MsgUploadStart',%this.name SPC "is saving an XPM file...");
			//saveXPMFile(%this,%filename);
			commandToClient(%this,'ReceiveXPMSaveRequest',%filename);
		}
	}
}

function serverCmdSetXPMStart(%this) {
	if(!isObject(%this.player)) {
		return;
	}

	%target = %this.player.getLookingAt();
	if(!isObject(%target)) {
		return;
	}

	%this.XPMStartPos = %target.getPosition();
	messageClient(%this,'',"Set XPM saving start position to" SPC %this.XPMStartPos);
}
function serverCmdSetXPMEnd(%this) {
	if(!isObject(%this.player)) {
		return;
	}

	%target = %this.player.getLookingAt();
	if(!isObject(%target)) {
		return;
	}

	%this.XPMEndPos = %target.getPosition();
	messageClient(%this,'',"Set XPM saving end position to" SPC %this.XPMEndPos);
}

function endXPMSave(%client) {
	for(%i=0;%i<$XPM::Linecount;%i++) {
		commandToClient(%client,'ReceiveXPMLine',$XPM::Line[%i]);
	}

	commandToClient(%client,'ReceiveXPMSaveDoneRequest');
	messageAll('MsgProcessComplete',"Generated XPM file with" SPC $XPM::Linecount SPC "lines in" SPC getTimeString((getRealTime() - $XPM::StartTime)/1000));

	deleteVariables("$XPM::*");
}

package XPMSupportPackage {
	function GameConnection::autoAdminCheck(%this) {
		commandToClient(%this,'SendXPMSupportHandshake');
		return parent::autoAdminCheck(%this);
	}
};
activatePackage(XPMSupportPackage);

function serverCmdOnReceiveXPMSupportHandshake(%this) {
	%this.supportsXPM = 1;
	messageClient(%this,'',"\c6The server is running\c3" SPC $GlobalXPM::Version SPC "\c6of the \c3XPM Support modification.");
}

function serverCmdOnReceiveSaveRequestAccepted(%this) {
	saveXPMFile(%this);
}