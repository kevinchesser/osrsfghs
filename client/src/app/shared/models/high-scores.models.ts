export interface TimeSpanLeaderboard {
    timeSpanLeaderboardItems: TimeSpanLeaderboardItem[]
    timeSpanUsed: string
}

export interface TimeSpanLeaderboardItem {
    character: Character
    timeSpanOverallXp: number
}

export interface XpDrop {
	skillId: number,
	xp: number,
	level: number,
	rank: number,
	timeStamp: Date
}

export interface CharacterOverview {
    character: Character
    xpDropsBySkill: Record<number, XpDrop[]>
}

export interface Character {
    name: string
    discordUserId: string
}
