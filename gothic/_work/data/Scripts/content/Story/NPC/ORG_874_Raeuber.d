instance ORG_874_Raeuber (Npc_Default)
{
	//-------- primary data --------
	
	name =					Name_Raeuber;
	Npctype =				NPCTYPE_ROGUE;
	guild =					GIL_NONE;      
	level =					12;

	
	voice =					13;
	id =					874;


	//-------- abilities --------

	attribute[ATR_STRENGTH] =		60;
	attribute[ATR_DEXTERITY] =		30;
	attribute[ATR_MANA_MAX] =		0;
	attribute[ATR_MANA] =			0;
	attribute[ATR_HITPOINTS_MAX] =	184;
	attribute[ATR_HITPOINTS] =		184;

	//-------- visuals --------
	// 				animations
	Mdl_SetVisual		(self,"HUMANS.MDS");
	Mdl_ApplyOverlayMds	(self,"Humans_Relaxed.mds");
	//				body mesh,				head mesh,				hairmesh,	face-tex,	hair-tex,	skin	
	Mdl_SetVisualBody (self,"hum_body_Naked0",0, 1,"Hum_Head_Bald", 38,  4, ORG_ARMOR_L);
	
	B_Scale (self);	
	Mdl_SetModelFatness (self, 0);		
	
	fight_tactic	=	FAI_HUMAN_STRONG;
	
	//-------- Talente ----------

	Npc_SetTalentSkill (self, NPC_TALENT_BOW,1);
	Npc_SetTalentSkill (self, NPC_TALENT_1H,1);
	
	
	//-------- inventory --------   
	                                 
	CreateInvItems (self, ItKeLockpick,1);		
	CreateInvItems(self, ItMiNugget, 11);
	CreateInvItems (self, ItFoRice,8);
	CreateInvItems (self, ItFoBooze, 3);
	CreateInvItems (self, ItLsTorch, 2);
	CreateInvItems (self, ItFo_Potion_Health_01, 1);
	CreateInvItem (self, ItMi_Stuff_Plate_01);
	CreateInvItem (self, ItMi_Stuff_Barbknife_01);
	CreateInvItem (self, ItFoLoaf);
	CreateInvItem (self, ItAt_Claws_01);
	EquipItem (self, ItMw_1H_Mace_03);
	EquipItem (self, ItRw_Bow_Long_01);
	CreateInvItems (self, ItAmArrow, 20);

	//-------------Daily Routine-------------
	daily_routine = Rtn_start_874;
	};

FUNC VOID Rtn_start_874 ()
{
    TA_StandAround		(13,00,14,00,"LOCATION_11_07");	
    TA_StandAround		(14,00,13,00,"LOCATION_11_07");
};

FUNC VOID Rtn_OMFull_874 ()
{
	TA_StandAround		(13,00,14,00,"LOCATION_11_07");	
    TA_StandAround		(14,00,13,00,"LOCATION_11_07");
};












