instance SLD_757_Soeldner (Npc_Default)
{
	//-------- primary data --------
	
	name =							Name_Soeldner;
	Npctype =						NPCTYPE_MINE_GUARD;
	guild =							GIL_SLD;       
	level =							18;
	
	voice =							11;
	id =							757;


	//-------- abilities --------

	attribute[ATR_STRENGTH] =		90;
	attribute[ATR_DEXTERITY] =		70;
	attribute[ATR_MANA_MAX] =		0;
	attribute[ATR_MANA] =			0;
	attribute[ATR_HITPOINTS_MAX] =	256;
	attribute[ATR_HITPOINTS] =		256;

	//-------- visuals --------
	// 				animations
	Mdl_SetVisual		(self,"HUMANS.MDS");
	Mdl_ApplyOverlayMds	(self,"Humans_Militia.mds");
	//				body mesh,				head mesh,				hairmesh,	face-tex,	hair-tex,	skin	
	Mdl_SetVisualBody (self,"hum_body_Naked0", 1, 1,"Hum_Head_Pony", 47, 1,SLD_ARMOR_H);
	
	B_Scale (self);
	Mdl_SetModelFatness (self, 0);
	
	fight_tactic	=	FAI_HUMAN_STRONG; 
	
	//-------- Talente --------

	Npc_SetTalentSkill (self, NPC_TALENT_1H,2);
	Npc_SetTalentSkill (self, NPC_TALENT_2H,1);
	Npc_SetTalentSkill (self, NPC_TALENT_BOW,1);	

	//-------- inventory --------                                    

	EquipItem (self, ItMw_1H_Mace_War_03);
	EquipItem (self, ItRw_Bow_Long_01);
	CreateInvItems(self, ItAmArrow, 20);
	CreateInvItems (self, ItFoRice,7);
	CreateInvItems (self, ItFoLoaf,5);
	CreateInvItems (self, ItFoMutton,4);
	CreateInvItems(self, ItMiNugget, 23);
	CreateInvItems (self, ItFoBooze,5);
	CreateInvItems (self, ItLsTorch,5);
	CreateInvItems (self, ItFo_Potion_Health_02,10);
	CreateInvItem (self, ItMi_Stuff_Barbknife_01);
	CreateInvItem (self, ItMi_Stuff_Amphore_01);
		

	//-------------Daily Routine-------------

	daily_routine = Rtn_FMCstart_757;

};

FUNC VOID Rtn_FMCstart_757 ()
{
	TA_GuardPalisade	(01,00,13,00,	"FMC_PATH12");
	TA_GuardPalisade	(13,00,01,00,	"FMC_PATH12");
};










