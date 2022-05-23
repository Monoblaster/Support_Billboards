datablock CameraData(BillboardLoadingCamera)
{
	mode = "Observer";
};
function BillboardLoadingCamera::OnTrigger(%data,%camera,%triggerNum,%triggerVal)
{
	%group = %camera.loading;
	
	if(isObject(%group))
	{
		%camera2 = %camera.getControllingObject();
		%client = %camera.getControllingClient();

		%group.FinishLoad();

		%client.setControlObject(%client.player);

		%camera2.delete();
		%camera.delete();
	}
}

datablock PlayerData(BillboardMount)
{
    shapeFile = "base/data/shapes/empty.dts";
	boundingBox = vectorScale("20 20 20", 4);
};

datablock PlayerData(OverheadBillboardMount : BillboardMount)
{
    shapeFile = "./billboardMount.dts";
};

datablock fxLightData(LoadingBillboard)
{
	LightOn = false;

	flareOn = true;
	flarebitmap = "base/data/shapes/blank.png";
	ConstantSize = 1;
    ConstantSizeOn = true;
    FadeTime = 0.000001;

	LinkFlare = false;
	blendMode = 1;
	flareColor = "1 0 0 1";

	AnimOffsets = true;
	startOffset = "0 0 0";
	endOffset = "0 0 0";
};

datablock fxLightData(LoadedBillboard)
{
	LightOn = false;

	flareOn = true;
	flarebitmap = "base/data/shapes/blank.png";
	ConstantSize = 1;
    ConstantSizeOn = true;
    FadeTime = inf;

	LinkFlare = false;
	blendMode = 1;
	flareColor = "1 0 0 1";

	AnimOffsets = true;
	startOffset = "0 0 0";
	endOffset = "0 0 0";
};

function Billboard_Create(%lightDB,%mountDB,%dontGhost)
{
	if(!isObject(%lightDB))
	{
		warn("Billboard_Create: " @ %lightDB @ " is not a valid fxLight datablock");
		return;
	}

	if(!isObject(%mountDB))
	{
		warn("Billboard_Create: " @ %mountDB @ " is not a valid player datablock");
		return;
	}

	if(%lightDB.getClassName() !$= "fxLightData")
	{
		warn("Billboard_Create: " @ %lightDB @ " is not a valid fxLight datablock");
		return;
	}

	if(%mountDB.getClassName() !$= "PlayerData")
	{
		warn("Billboard_Create: " @ %mountDB @ " is not a valid player datablock");
		return;
	}

	%light = new fxLight()
	{
		dataBlock = %lightDB;
	};

	//move it up so it doesn't crash the game
	%mount = new aiPlayer()
	{
		dataBlock = %mountDB;
		position = "0 0 999999";
	};
	
	%mount.setDamageLevel(10000);
	%mount.setTransform("0 0 0");

	%mount.light = %light;
	%light.attachToObject(%mount);
	

	%light.setNetFlag(6,true);
	%mount.setNetFlag(6,true);
	if(!%dontGhost)
	{
		//make sure it scopes just because it has to manually be done with these flags
		%light.setScopeAlways();
		%mount.setScopeAlways();
	}
	else
	{
		//disable ghosting automaticaly
		%light.setNetFlag(8,false);
		%mount.setNetFlag(8,false);
	}

	return %mount;
}

function Billboard_Ghost(%billboard,%client)
{
	if(%client $= "ALL")
	{
		%group = ClientGroup;
		%count = %group.getCount();
		for(%i = 0; %i < %count; %i++)
		{	
			%client = %group.getObject(%i);
			Billboard_Ghost(%billboard,%client);
		}
		return %billboard;
	}

	if(!isObject(%billboard) || !isObject(%billboard.light))
	{
		warn("Billboard_Ghost: " @ %billboard @ " is not a valid billboard");
		return;
	}

	if(%billboard.getClassName() !$= "aiPlayer" && %billboard.light.getClassName() $= "fxLight")
	{
		warn("Billboard_Ghost: " @ %billboard @ " is not a valid billboard");
		return;
	}

	if(!isObject(%client))
	{
		warn("Billboard_Ghost: " @ %client @ " is not a valid client");
		return;
	}

	if(%client.getClassName() !$= "GameConnection")
	{
		warn("Billboard_Ghost: " @ %client @ " is not a valid client");
		return;
	}

	%billboard.ScopeToClient(%client);
	%billboard.light.ScopeToClient(%client);

	return %billboard;
}

function Billboard_ClearGhost(%billboard,%client)
{
	if(%client $= "ALL")
	{
		%group = ClientGroup;
		%count = %group.getCount();
		for(%i = 0; %i < %count; %i++)
		{	
			%client = %group.getObject(%i);
			Billboard_ClearGhost(%billboard,%client);
		}
		return %billboard;
	}

	if(!isObject(%billboard) || !isObject(%billboard.light))
	{
		warn("Billboard_ClearGhost: " @ %billboard @ " is not a valid billboard");
		return;
	}

	if(%billboard.getClassName() !$= "aiPlayer" && %billboard.light.getClassName() $= "fxLight")
	{
		warn("Billboard_ClearGhost: " @ %billboard @ " is not a valid billboard");
		return;
	}

	if(!isObject(%client))
	{
		warn("Billboard_ClearGhost: " @ %client @ " is not a valid client");
		return;
	}

	if(%client.getClassName() !$= "GameConnection")
	{
		warn("Billboard_ClearGhost: " @ %client @ " is not a valid client");
		return;
	}

	%billboard.ClearScopeToClient(%client);
	%billboard.light.ClearScopeToClient(%client);

	return %billboard;
}

function Billboard_Delete(%billboard)
{	
	if(!isObject(%billboard) || !isObject(%billboard.light))
	{
		warn("Billboard_Delete: " @ %billboard @ " is not a valid billboard");
		return;
	}

	if(%billboard.getClassName() !$= "aiPlayer" && %billboard.light.getClassName() $= "fxLight")
	{
		warn("Billboard_Delete: " @ %billboard @ " is not a valid billboard");
		return;
	}

	%billboard.light.delete();
	%billboard.delete();
}

function AVBillboards_Create(%mountDB,%count)
{
	if(!isObject(%mountDB))
	{
		warn("AVBillboards_Create: " @ %mountDB @ " is not a valid player datablock");
		return;
	}

	if(%mountDB.getClassName() !$= "PlayerData")
	{
		warn("AVBillboards_Create: " @ %mountDB @ " is not a valid player datablock");
		return;
	}

	%group = new scriptGroup()
	{
		class = "AVBillboards";
	};

	for(%i = 0; %i < %count; %i++)
	{
		%billboard = Billboard_Create("LoadingBillboard",%mountDB,true);
		%group.add(%billboard);
	}

	return %group;
}

function AVBillboards::Load(%group,%client,%pos)
{
	if(%group.loadedClient !$= "")
	{
		warn("AVBillboards::Load: " @ %grooup @ " already loaded to client " @ %group.loadedClient);
		return;
	}

	%camera = new Camera(){dataBlock = BillboardLoadingCamera;};
	%dummycamera = new Camera(){dataBlock = BillboardLoadingCamera; loading = %group;};
	%camera.setTransform(%pos SPC "0 0 0 0");
	%client.setControlObject(%camera);
	%camera.setcontrolObject(%dummycamera);

	
	%loadingMount = new aiPlayer()
	{
		dataBlock = "BillboardMount";
		position = vectorAdd(%pos,"0 10 0");
	};
	talk(%loadingMount);

	%loadingMount.setDamageLevel(10000);
	
	%loadingMount.setNetFlag(8,false);
	%loadingMount.setNetFlag(6,true);
	%loadingMount.ScopeToClient(%client);

	%count = %group.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%billboard = %group.getObject(%i);
		billboard_ghost(%billboard,%client);
		%billboard.light.attachToObject(%loadingMount);
	}

	%group.loadedClient = %client;
}

function AVBillboards::FinishLoad(%group)
{
	for(%i = 0; %i < %group.getCount(); %i++)
	{
		%billboard = %group.getObject(%i);

		%billboard.light.setNetFlag(8,true);
		%billboard.light.setDatablock(LoadedBillboard);
		%billboard.light.setEnable(false);
		%billboard.light.attachToObject(%billboard);
		%billboard.light.setNetFlag(8,false);
	}
}

function AVBillboards::Make(%group,%lightDB,%position,%tag)
{
	if(!isObject(%lightDB))
	{
		warn("AVBillboards::Make: " @ %lightDB @ " is not a valid fxLight datablock");
		return;
	}

	if(%lightDB.getClassName() !$= "fxLightData")
	{
		warn("AVBillboards::Make: " @ %lightDB @ " is not a valid fxLight datablock");
		return;
	}

	%active = %group.active;
	if(%active >= %group.getCount())
	{
		warn("AVBillboards::Make: " @ %group @ " group over count");
		return;
	}
	
	//get an inactive billboard
	%billboard = %group.getObject(%active);
	%billboard.tag = %tag;
	%group.active++;

	//set it's datablock and enable
	%billboard.light.setNetFlag(8,true);
	%billboard.light.setDatablock(%lightDB);
	%billboard.light.setEnable(true);
	%billboard.light.setNetFlag(8,false);

	%billboard.setTransform(%position);

	return %billboard;
}

function AVBillboards::Clear(%group,%tag)
{
	%count = %group.getCount();
	for(%i = %count - 1; %i >= 0; %i--)
	{
		%billboard = %group.getObject(%i);
		if(%tag !$= "" && %tag !$= %billboard.tag)
		{
			continue;
		}

		//disable all of the lights and unmount if mounted
		%mount = %billboard.getObjectMount();
		if(isObject(%mount))
		{
			%mount.unmountObject(%billboard);
		}

		%billboard.light.setNetFlag(8,true);
		%billboard.light.setEnable(false);
		%billboard.light.setNetFlag(8,false);
		//remove old tag
		%billboard.tag = "";

		//push to back so only active billboards are in front
		%group.pushToBack(%billboard);
		%group.active--;
	}

	return %group;
}

function AVBillboards::Delete(%group)
{
	%group.deleteAll();
	Parent::delete(%group);
}