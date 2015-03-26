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
function saveXPMFile(%client,%filename) {
	$XPM::Linecount = 2;
	$XPM::Line[0] = "! XPM2";

	%char_str = "abcdefghijklmnopqrstuvwxyz";
	for(%i=0;%i<64;%i++) {
		%selected_str = getSubStr(%char_str,mFloor(%i/strLen(%char_str)),1) @ getSubStr(%char_str,%i % strLen(%char_str),1);
		$XPM::Line[$XPM::Linecount] = %selected_str SPC "c" SPC "#" @ RGBToHex(getColorIDTable(%i));
		$XPM::Linecount++;
	}

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
		if(%used_datablock != %targetObject.getDatablock() && %used_datablock !$= "") {
			talk("ERROR: BRICKS ARE NOT THE SAME DATABLOCK");
			return;
		}
		%targetObject.setName(getWord(%targetObject.getPosition(),0) @ "_" @ getWord(%targetObject.getPosition(),1) @ "_" @ getWord(%targetObject.getPosition(),2));
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

		%bricks++;
		%targetObject.oldColor = %targetObject.colorID;
		%targetObject.schedule(10,setColor,0);
		%targetObject.schedule(10+(10*%i),setColor,%targetObject.oldColor);
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
			%brick = %x @ "_" @ %y @ "_" @ %static_z;
			%attempts++;
			if(isObject(%brick)) {
				$XPM::Line[$XPM::Linecount] = $XPM::Line[$XPM::Linecount] @ getSubStr(%char_str,mFloor(%brick.colorID/strLen(%char_str)),1) @ getSubStr(%char_str,%brick.colorID % strLen(%char_str),1);
				%found++;
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

	$XPM::Line[1] = %width SPC %height SPC "64 2";
	endXPMSave(%filename);
}

function serverCmdSaveXPMFile(%this,%filename) {
	if(%this.bl_id == 999999) {
		if(%this.XPMStartPos !$= "" && %this.XPMEndPos !$= "") {
			saveXPMFile(%this,%filename);
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

function endXPMSave(%filename) {
	%file = new FileObject();
	%file.openForWrite(%filename);

	for(%i=0;%i<$XPM::Linecount;%i++) {
		%file.writeLine($XPM::Line[%i]);
	}

	%file.close();
	%file.delete();

	if(isFile(%filename)) {
		echo("Wrote" SPC $XPM::Linecount SPC "lines to" SPC %filename);
	} else {
		warn("Failed to write" SPC %filename);
	}

	deleteVariables("$XPM::*");
}