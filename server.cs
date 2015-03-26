$GlobalXPM::Version = "v0.3.1-1";

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

function fxDTSBrick::getBoxSize(%this) {
	%box = %this.getWorldBox();
	%size_x = getWord(%box,3) - getWord(%box,0);
	%size_y = getWord(%box,4) - getWord(%box,1);
	%size_z = getWord(%box,5) - getWord(%box,2);

	return %size_x SPC %size_y SPC %size_z;
}

function isBetween(%min,%max,%num) {
	if(%num > %min && %num < %max) {
		return 1;
	} else {
		return 0;
	}
}

function fxDTSBrick::isPosInsideBrick(%this,%pos) {
	%box = %this.getWorldBox();
	%brick_x[min] = getWord(%box,0);
	%brick_y[min] = getWord(%box,1);
	%brick_z[min] = getWord(%box,2);
	%brick_x[max] = getWord(%box,3);
	%brick_y[max] = getWord(%box,4);
	%brick_z[max] = getWord(%box,5);
	if(isBetween(%brick_x[min],%brick_x[max],getWord(%pos,0))) {
		if(isBetween(%brick_y[min],%brick_y[max],getWord(%pos,1))) {
			if(isBetween(%brick_z[min],%brick_z[max],getWord(%pos,2))) {
				return 1;
			}
		}
	}
	return 0;
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
		%color_used = 0;
		%static_z = getWord(%targetObject.getPosition(),2);

		%x_pos_h = getWord(%targetObject.getWorldBox(),3);
		%y_pos_h = getWord(%targetObject.getWorldBox(),4);
		%x_pos_l = getWord(%targetObject.getWorldBox(),0);
		%y_pos_l = getWord(%targetObject.getWorldBox(),1);
		if(%x_pos_h > %highest_x) {
			%highest_x = %x_pos_h;
		}
		if(%x_pos_l < %lowest_x) {
			%lowest_x = %x_pos_l;
		}
		if(%y_pos_h > %highest_y) {
			%highest_y = %y_pos_h;
		}
		if(%y_pos_l < %lowest_y) {
			%lowest_y = %y_pos_l;
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
	}

	%char_str = "abcdefghijklmnopqrstuvwxyz";
	for(%i=0;%i<$XPM::UsedColors;%i++) {
		%selected_str = getSubStr(%char_str,mFloor($XPM::Color[%i]/strLen(%char_str)),1) @ getSubStr(%char_str,$XPM::Color[%i] % strLen(%char_str),1);
		$XPM::Line[$XPM::Linecount] = %selected_str SPC "c" SPC "#" @ RGBToHex(getColorIDTable($XPM::Color[%i]));
		$XPM::Linecount++;
	}

	%increment_x = %increment_y = 0.5;

	%attempts = 0;
	%width = 0;
	%height = 0;
	%loop_start = getRealTime();

	for(%x=%lowest_x;%x<%highest_x;%x+=%increment_x) {
		%width = 0;
		%found = 0;
		for(%y=%lowest_y;%y<%highest_y;%y+=%increment_y) {
			%found = 0;
			%brick = "_" @ %x @ "_" @ %y @ "_" @ %static_z;
			%attempts++;

			%pos = %x SPC %y SPC %static_z;
			// 1x1 plate = 0.5 0.5 0.2
			initContainerBoxSearch(vectorAdd(%pos,"0.25 0.25 0"),"0 0 0",$TypeMasks::FXBrickObjectType);
			while((%targetObject = containerSearchNext()) != 0 && isObject(%targetObject)) {
				$XPM::Line[$XPM::Linecount] = $XPM::Line[$XPM::Linecount] @ getSubStr(%char_str,mFloor(%targetObject.colorID/strLen(%char_str)),1) @ getSubStr(%char_str,%targetObject.colorID % strLen(%char_str),1);
				%width++;
				%found = 1;
				break;
			}
			if(!%found) {
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

	//%this.XPMStartPos = %target.getPosition();
	%this.XPMStartPos = getWords(%target.getWorldBox(),0,2);
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

	//%this.XPMEndPos = %target.getPosition();
	%this.XPMEndPos = getWords(%target.getWorldBox(),0,2);
	messageClient(%this,'',"Set XPM saving end position to" SPC %this.XPMEndPos);
}

function endXPMSave(%client) {
	for(%i=0;%i<$XPM::Linecount;%i++) {
		// torque has a hard limit at 256
		if(strLen($XPM::Line[%i]) > 255) {
			%temp = strLen($XPM::Line[%i]);
			%j = 0;
			while(%j < %temp) {
				commandToClient(%client,'ReceiveXPMMultiLine',getSubStr($XPM::Line[%i],%j,%j+255));
				%j += 255;
			}
			commandToClient(%client,'ReceiveXPMMultiLineFinish');
		} else {
			commandToClient(%client,'ReceiveXPMLine',$XPM::Line[%i]);
		}
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