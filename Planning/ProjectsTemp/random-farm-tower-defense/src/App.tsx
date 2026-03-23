import React, { useState, useEffect } from 'react';
import { 
  Coins, 
  Carrot, 
  Apple, 
  Grape, 
  Heart, 
  Swords, 
  ShoppingCart, 
  TrendingUp, 
  Info, 
  Menu,
  X,
  ChevronRight,
  Shield,
  Zap,
  Hammer,
  BookOpen,
  RefreshCcw
} from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';
import { 
  LineChart, 
  Line, 
  XAxis, 
  YAxis, 
  CartesianGrid, 
  Tooltip, 
  ResponsiveContainer 
} from 'recharts';
import { cn } from './lib/utils';

// --- Types ---

type PopupType = 'shop' | 'market' | 'farm' | 'menu' | 'guide' | null;
type GuideTab = 'howToPlay' | 'shopInfo' | 'towerTiers';
type ShopInfoTab = 'weapons' | 'buffs';

interface Tower {
  id: string;
  name: string;
  tier: number;
  image: string;
  stats: {
    health: number;
    damage: number;
    attackSpeed: string;
    moveSpeed: string;
    damageType: string;
    armor: number;
    armorType: string;
  };
  skill: {
    name: string;
    icon: React.ReactNode;
    description: string;
  };
}

interface Weapon {
  id: string;
  name: string;
  image: string;
  description: string;
  stats: string;
}

interface Buff {
  id: string;
  name: string;
  image: string;
  description: string;
  effect: string;
}

const WEAPONS_DATA: Weapon[] = [
  { id: 'w1', name: 'Steel Blade', image: 'https://picsum.photos/seed/steel_blade/200/200', description: 'A sharp blade that increases raw physical damage.', stats: '+15 Physical Damage' },
  { id: 'w2', name: 'Magic Staff', image: 'https://picsum.photos/seed/magic_staff/200/200', description: 'Enhances magical abilities and mana regeneration.', stats: '+20 Magic Power' },
  { id: 'w3', name: 'Heavy Hammer', image: 'https://picsum.photos/seed/heavy_hammer/200/200', description: 'Slow but hits like a truck. Great for breaking armor.', stats: '+30 Siege Damage' },
  { id: 'w4', name: 'Long Bow', image: 'https://picsum.photos/seed/long_bow/200/200', description: 'Increases range and critical strike chance.', stats: '+10% Range, +5% Crit' },
  { id: 'w5', name: 'Dagger', image: 'https://picsum.photos/seed/dagger/200/200', description: 'Fast attacks with high critical potential.', stats: '+25% Attack Speed' },
  { id: 'w6', name: 'Crossbow', image: 'https://picsum.photos/seed/crossbow/200/200', description: 'High penetration bolts.', stats: '+10 Armor Pen' },
  { id: 'w7', name: 'Fire Wand', image: 'https://picsum.photos/seed/fire_wand/200/200', description: 'Deals burn damage over time.', stats: 'Burn Effect' },
  { id: 'w8', name: 'Ice Axe', image: 'https://picsum.photos/seed/ice_axe/200/200', description: 'Chills enemies on hit.', stats: '15% Slow on Hit' },
  { id: 'w9', name: 'Poison Dart', image: 'https://picsum.photos/seed/poison_dart/200/200', description: 'Stacking poison damage.', stats: 'Poison Stacks' },
  { id: 'w10', name: 'Shield Bash', image: 'https://picsum.photos/seed/shield_bash/200/200', description: 'Defensive weapon that can stun.', stats: '+5 Armor, Stun Chance' },
];

const BUFFS_DATA: Buff[] = [
  { id: 'b1', name: 'Haste Potion', image: 'https://picsum.photos/seed/haste/200/200', description: 'Temporarily increases the attack speed of all towers.', effect: '+25% Attack Speed' },
  { id: 'b2', name: 'Fortify Scroll', image: 'https://picsum.photos/seed/fortify/200/200', description: 'Strengthens tower defenses for a short duration.', effect: '+10 Armor' },
  { id: 'b3', name: 'Gold Magnet', image: 'https://picsum.photos/seed/magnet/200/200', description: 'Increases gold dropped by enemies.', effect: '+20% Gold Drop' },
  { id: 'b4', name: 'Frost Aura', image: 'https://picsum.photos/seed/frost/200/200', description: 'Slows down all enemies on the path.', effect: '30% Slow' },
  { id: 'b5', name: 'Rage Blood', image: 'https://picsum.photos/seed/rage/200/200', description: 'Massive damage boost but reduces health.', effect: '+50% Damage' },
  { id: 'b6', name: 'Holy Light', image: 'https://picsum.photos/seed/holy/200/200', description: 'Heals the farm base over time.', effect: '+5 HP/sec' },
  { id: 'b7', name: 'Wind Walk', image: 'https://picsum.photos/seed/wind/200/200', description: 'Towers ignore enemy armor for 10s.', effect: 'Armor Ignore' },
  { id: 'b8', name: 'Earthquake', image: 'https://picsum.photos/seed/earth/200/200', description: 'Shakes the ground, slowing and damaging.', effect: 'AoE Damage + Slow' },
  { id: 'b9', name: 'Midas Touch', image: 'https://picsum.photos/seed/midas/200/200', description: 'Next 5 kills give double gold.', effect: '2x Gold' },
  { id: 'b10', name: 'Mirror Image', image: 'https://picsum.photos/seed/mirror/200/200', description: 'Creates temporary illusions of towers.', effect: '+2 Illusions' },
];

const TOWERS_DATA: Record<number, Tower[]> = {
  1: [
    {
      id: 't1_1',
      name: 'Archer Tower',
      tier: 1,
      image: 'https://picsum.photos/seed/archer_tower/300/300',
      stats: { health: 200, damage: 15, attackSpeed: 'Fast', moveSpeed: 'None', damageType: 'Pierce', armor: 5, armorType: 'Light' },
      skill: { name: 'Double Shot', icon: <Zap className="w-4 h-4" />, description: 'Fires two arrows at once every 5th attack.' }
    },
    {
      id: 't1_2',
      name: 'Cannon Tower',
      tier: 1,
      image: 'https://picsum.photos/seed/cannon_tower/300/300',
      stats: { health: 350, damage: 40, attackSpeed: 'Slow', moveSpeed: 'None', damageType: 'Siege', armor: 15, armorType: 'Heavy' },
      skill: { name: 'Splash Damage', icon: <Swords className="w-4 h-4" />, description: 'Deals 50% damage to nearby enemies.' }
    },
    {
      id: 't1_3',
      name: 'Mage Tower',
      tier: 1,
      image: 'https://picsum.photos/seed/mage_tower/300/300',
      stats: { health: 150, damage: 25, attackSpeed: 'Medium', moveSpeed: 'None', damageType: 'Magic', armor: 2, armorType: 'Unarmored' },
      skill: { name: 'Arcane Bolt', icon: <Zap className="w-4 h-4" />, description: 'Ignores 20% of enemy armor.' }
    }
  ],
  2: [
    {
      id: 't2_1',
      name: 'Sniper Tower',
      tier: 2,
      image: 'https://picsum.photos/seed/sniper_tower/300/300',
      stats: { health: 250, damage: 80, attackSpeed: 'Very Slow', moveSpeed: 'None', damageType: 'Pierce', armor: 8, armorType: 'Medium' },
      skill: { name: 'Headshot', icon: <Swords className="w-4 h-4" />, description: '5% chance to deal 4x damage.' }
    },
    {
      id: 't2_2',
      name: 'Mortar Tower',
      tier: 2,
      image: 'https://picsum.photos/seed/mortar_tower/300/300',
      stats: { health: 400, damage: 60, attackSpeed: 'Very Slow', moveSpeed: 'None', damageType: 'Siege', armor: 20, armorType: 'Heavy' },
      skill: { name: 'Stun Shell', icon: <Hammer className="w-4 h-4" />, description: 'Briefly stuns enemies in the blast radius.' }
    },
    {
      id: 't2_3',
      name: 'Ice Mage',
      tier: 2,
      image: 'https://picsum.photos/seed/ice_mage/300/300',
      stats: { health: 200, damage: 30, attackSpeed: 'Medium', moveSpeed: 'None', damageType: 'Magic', armor: 5, armorType: 'Light' },
      skill: { name: 'Frostbite', icon: <Zap className="w-4 h-4" />, description: 'Slows enemy movement by 30% for 2 seconds.' }
    },
    {
      id: 't2_4',
      name: 'Poison Tower',
      tier: 2,
      image: 'https://picsum.photos/seed/poison_tower/300/300',
      stats: { health: 220, damage: 10, attackSpeed: 'Fast', moveSpeed: 'None', damageType: 'Magic', armor: 6, armorType: 'Light' },
      skill: { name: 'Toxic Cloud', icon: <Grape className="w-4 h-4" />, description: 'Deals damage over time to enemies.' }
    },
    {
      id: 't2_5',
      name: 'Lightning Tower',
      tier: 2,
      image: 'https://picsum.photos/seed/lightning_tower/300/300',
      stats: { health: 180, damage: 45, attackSpeed: 'Fast', moveSpeed: 'None', damageType: 'Magic', armor: 4, armorType: 'Unarmored' },
      skill: { name: 'Chain Lightning', icon: <Zap className="w-4 h-4" />, description: 'Lightning jumps to 3 additional targets.' }
    }
  ]
};

interface StatProps {
  icon: React.ReactNode;
  value: string | number;
  label?: string;
  color?: string;
}

// --- Mock Data ---

const MARKET_DATA = [
  { time: '10m', price: 120 },
  { time: '60m', price: 150 },
  { time: '80m', price: 130 },
  { time: '120m', price: 180 },
  { time: '22h', price: 160 },
  { time: '24h', price: 210 },
  { time: '15h', price: 140 },
  { time: '36h', price: 250 },
];

const CARDS = [
  { id: 1, name: 'Random Tower', level: 1, type: 'tower', color: 'bg-gray-600' },
  { id: 2, name: 'Random Tower', level: 1, type: 'tower', color: 'bg-gray-600' },
  { id: 3, name: 'Random Tower', level: 1, type: 'tower', color: 'bg-gray-600' },
  { id: 4, name: 'Random Tower', level: 1, type: 'tower', color: 'bg-gray-600' },
  { id: 5, name: 'Random Tower', level: 5, type: 'tower', color: 'bg-yellow-600' },
  { id: 6, name: 'Random Tower', level: 5, type: 'tower', color: 'bg-yellow-600' },
  { id: 7, name: 'Reinforced Axe', level: 1, type: 'weapon', color: 'bg-red-900' },
  { id: 8, name: 'Speed Boost', level: 1, type: 'buff', color: 'bg-green-800' },
];

// --- Components ---

const StatItem = ({ icon, value, label, color }: StatProps) => (
  <div className="flex items-center gap-2 px-3 py-1 bg-black/40 rounded-full border border-white/10">
    <span className={cn("w-5 h-5 flex items-center justify-center", color)}>{icon}</span>
    <div className="flex flex-col leading-none">
      <span className="text-sm font-bold">{value}</span>
      {label && <span className="text-[10px] opacity-50 uppercase">{label}</span>}
    </div>
  </div>
);

const PopupWrapper = ({ title, onClose, children, className }: { title: string, onClose: () => void, children: React.ReactNode, className?: string, key?: string }) => (
  <motion.div 
    initial={{ opacity: 0, scale: 0.9, x: 20 }}
    animate={{ opacity: 1, scale: 1, x: 0 }}
    exit={{ opacity: 0, scale: 0.9, x: 20 }}
    className={cn("game-panel w-[450px] max-h-[80vh] flex flex-col pointer-events-auto", className)}
  >
    <div className="flex items-center justify-between p-4 border-b border-game-border bg-game-wood/50">
      <div className="flex items-center gap-2">
        <div className="w-2 h-2 bg-game-gold rounded-full animate-pulse" />
        <h2 className="text-xl font-bold uppercase tracking-widest gold-text">{title}</h2>
      </div>
      <button onClick={onClose} className="p-1 hover:bg-white/10 rounded transition-colors">
        <X className="w-6 h-6 text-game-gold" />
      </button>
    </div>
    <div className="flex-1 overflow-y-auto p-4 scrollbar-hide">
      {children}
    </div>
  </motion.div>
);

export default function App() {
  const [activePopup, setActivePopup] = useState<PopupType>(null);
  const [guideTab, setGuideTab] = useState<GuideTab>('howToPlay');
  const [shopInfoTab, setShopInfoTab] = useState<ShopInfoTab>('weapons');
  const [selectedTower, setSelectedTower] = useState<Tower | null>(null);
  const [selectedItem, setSelectedItem] = useState<Weapon | Buff | null>(null);
  const [gold, setGold] = useState(125);
  const [totalSeeds, setTotalSeeds] = useState(50);
  const [shopWeapons, setShopWeapons] = useState([1, 2, 3, 4]);
  const [shopBuffs, setShopBuffs] = useState([1, 2, 3, 4]);
  const [crops, setCrops] = useState({
    carrots: { harvested: 15, growing: 2 },
    pumpkins: { harvested: 8, growing: 1 },
    grapes: { harvested: 3, growing: 0 }
  });

  const REROLL_COST = 5;

  const rerollWeapons = () => {
    if (gold >= REROLL_COST) {
      setGold(prev => prev - REROLL_COST);
      setShopWeapons(prev => prev.map(() => Math.floor(Math.random() * 1000)));
    }
  };

  const rerollBuffs = () => {
    if (gold >= REROLL_COST) {
      setGold(prev => prev - REROLL_COST);
      setShopBuffs(prev => prev.map(() => Math.floor(Math.random() * 1000)));
    }
  };

  const plantCrop = (type: keyof typeof crops) => {
    if (totalSeeds > 0) {
      setTotalSeeds(prev => prev - 1);
      setCrops(prev => ({ 
        ...prev, 
        [type]: { ...prev[type], growing: prev[type].growing + 1 } 
      }));
    }
  };

  return (
    <div className="relative w-full h-screen bg-[#1a1a1a] text-white overflow-hidden flex flex-col">
      {/* Background Image Placeholder */}
      <div 
        className="absolute inset-0 opacity-40 pointer-events-none bg-cover bg-center"
        style={{ backgroundImage: 'url("https://picsum.photos/seed/towerdefense/1920/1080?blur=5")' }}
      />

      {/* Top Bar */}
      <div className="relative z-10 flex items-center justify-between p-4 bg-gradient-to-b from-black/80 to-transparent">
        <div className="flex items-center gap-4">
          <StatItem icon={<Coins className="w-4 h-4" />} value={gold} color="text-yellow-400" />
          <StatItem icon={<Carrot className="w-4 h-4" />} value="26" color="text-orange-500" />
          <StatItem icon={<Apple className="w-4 h-4" />} value="10" color="text-red-500" />
          <StatItem icon={<Grape className="w-4 h-4" />} value="5" color="text-purple-500" />
          <StatItem icon={<Hammer className="w-4 h-4" />} value="30" color="text-amber-700" />
        </div>
        
        <div className="absolute left-1/2 -translate-x-1/2 flex flex-col items-center">
          <h1 className="text-2xl font-black uppercase tracking-tighter gold-text italic">Random Farm</h1>
          <div className="text-xs opacity-50 font-mono">EST. 2026</div>
        </div>

        <div className="flex items-center gap-4">
          <StatItem icon={<Heart className="w-4 h-4" />} value="110" label="Base HP" color="text-red-500" />
          <div className="px-4 py-1 bg-game-wood border border-game-gold/30 rounded text-sm font-bold uppercase">
            Wave 13
          </div>
        </div>
      </div>

      {/* Main Game Area */}
      <div className="flex-1 relative flex">
        {/* Game World Placeholder */}
        <div className="flex-1 flex items-center justify-center">
          <div className="relative w-[80%] aspect-video game-panel overflow-hidden rounded-3xl border-4 border-game-border/50">
             <img 
               src="https://picsum.photos/seed/isometric-farm/1200/800" 
               alt="Game World" 
               className="w-full h-full object-cover opacity-80"
               referrerPolicy="no-referrer"
             />
             <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent" />
             
             {/* Floating UI elements in world */}
             <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
                <div className="game-panel p-2 rounded-lg bg-black/60 border-game-gold/50 flex flex-col items-center gap-1">
                   <div className="w-12 h-12 bg-game-wood rounded border border-game-gold/30 flex items-center justify-center">
                      <Shield className="w-8 h-8 text-game-gold" />
                   </div>
                   <div className="text-[10px] font-bold uppercase text-game-gold">Archer Tower</div>
                   <div className="w-16 h-1 bg-gray-800 rounded-full overflow-hidden">
                      <div className="w-3/4 h-full bg-green-500" />
                   </div>
                </div>
             </div>
          </div>
        </div>

        {/* Right Sidebar */}
        <div className="w-20 flex flex-col gap-4 p-4 items-center justify-center relative z-20">
          <button 
            onClick={() => setActivePopup(activePopup === 'shop' ? null : 'shop')}
            className={cn("w-14 h-14 game-button rounded-xl flex flex-col items-center justify-center gap-1", activePopup === 'shop' && "ring-2 ring-game-gold")}
          >
            <ShoppingCart className="w-6 h-6" />
            <span className="text-[8px] font-bold uppercase">Shop</span>
          </button>
          <button 
            onClick={() => setActivePopup(activePopup === 'market' ? null : 'market')}
            className={cn("w-14 h-14 game-button rounded-xl flex flex-col items-center justify-center gap-1", activePopup === 'market' && "ring-2 ring-game-gold")}
          >
            <TrendingUp className="w-6 h-6" />
            <span className="text-[8px] font-bold uppercase">Market</span>
          </button>
          <button 
            onClick={() => setActivePopup(activePopup === 'farm' ? null : 'farm')}
            className={cn("w-14 h-14 game-button rounded-xl flex flex-col items-center justify-center gap-1", activePopup === 'farm' && "ring-2 ring-game-gold")}
          >
            <Carrot className="w-6 h-6" />
            <span className="text-[8px] font-bold uppercase">Farm</span>
          </button>
          <button 
            onClick={() => setActivePopup(activePopup === 'guide' ? null : 'guide')}
            className={cn("w-14 h-14 game-button rounded-xl flex flex-col items-center justify-center gap-1", activePopup === 'guide' && "ring-2 ring-game-gold")}
          >
            <BookOpen className="w-6 h-6" />
            <span className="text-[8px] font-bold uppercase">Guide</span>
          </button>
          <div className="flex-1" />
          <button 
            onClick={() => setActivePopup(activePopup === 'menu' ? null : 'menu')}
            className="w-14 h-14 game-button rounded-xl flex flex-col items-center justify-center gap-1"
          >
            <Menu className="w-6 h-6" />
            <span className="text-[8px] font-bold uppercase">Menu</span>
          </button>
        </div>

        {/* Popups Overlay */}
        <div className="absolute inset-0 pointer-events-none flex items-center justify-end pr-24 z-30">
          <AnimatePresence mode="wait">
            {activePopup === 'shop' && (
              <PopupWrapper key="shop" title="Shop" onClose={() => setActivePopup(null)}>
                <div className="space-y-6">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="game-panel p-3 bg-black/40 border-game-gold/20 rounded-lg flex flex-col items-center gap-2">
                      <div className="w-16 h-16 bg-game-wood rounded flex items-center justify-center border border-game-gold/30">
                        <Shield className="w-10 h-10 text-gray-400" />
                      </div>
                      <div className="text-center">
                        <div className="text-xs font-bold uppercase">Level 1 Random Tower Pack</div>
                        <div className="text-[10px] opacity-50">(10 Gold)</div>
                      </div>
                      <button className="game-button px-4 py-1 rounded text-xs w-full flex items-center justify-center gap-1">
                        <Coins className="w-3 h-3" /> 10
                      </button>
                    </div>
                    <div className="game-panel p-3 bg-black/40 border-game-gold/20 rounded-lg flex flex-col items-center gap-2">
                      <div className="w-16 h-16 bg-game-wood rounded flex items-center justify-center border border-game-gold/30">
                        <Shield className="w-10 h-10 text-game-gold" />
                      </div>
                      <div className="text-center">
                        <div className="text-xs font-bold uppercase">Level 5 Random Tower Pack</div>
                        <div className="text-[10px] opacity-50">(100 Gold)</div>
                      </div>
                      <button className="game-button px-4 py-1 rounded text-xs w-full flex items-center justify-center gap-1">
                        <Coins className="w-3 h-3" /> 100
                      </button>
                    </div>
                  </div>

                  <div>
                    <div className="flex items-center justify-between border-b border-game-border mb-3 pb-1">
                      <h3 className="text-sm font-bold uppercase gold-text">Weapons</h3>
                      <button 
                        onClick={rerollWeapons}
                        disabled={gold < REROLL_COST}
                        className={cn(
                          "flex items-center gap-1 text-[10px] uppercase font-bold text-game-gold hover:brightness-125 transition-all",
                          gold < REROLL_COST && "opacity-50 cursor-not-allowed grayscale"
                        )}
                      >
                        <RefreshCcw className="w-3 h-3" /> Reroll ({REROLL_COST} Gold)
                      </button>
                    </div>
                    <div className="grid grid-cols-4 gap-2">
                      {shopWeapons.map(i => (
                        <motion.div 
                          key={i} 
                          initial={{ scale: 0.8, opacity: 0 }}
                          animate={{ scale: 1, opacity: 1 }}
                          className="aspect-square game-panel bg-black/40 rounded border-game-gold/10 flex items-center justify-center hover:border-game-gold/50 cursor-pointer group"
                        >
                          <Swords className="w-6 h-6 opacity-30 group-hover:opacity-100 transition-opacity" />
                        </motion.div>
                      ))}
                    </div>
                  </div>

                  <div>
                    <div className="flex items-center justify-between border-b border-game-border mb-3 pb-1">
                      <h3 className="text-sm font-bold uppercase gold-text">Buffs</h3>
                      <button 
                        onClick={rerollBuffs}
                        disabled={gold < REROLL_COST}
                        className={cn(
                          "flex items-center gap-1 text-[10px] uppercase font-bold text-game-gold hover:brightness-125 transition-all",
                          gold < REROLL_COST && "opacity-50 cursor-not-allowed grayscale"
                        )}
                      >
                        <RefreshCcw className="w-3 h-3" /> Reroll ({REROLL_COST} Gold)
                      </button>
                    </div>
                    <div className="grid grid-cols-4 gap-2">
                      {shopBuffs.map(i => (
                        <motion.div 
                          key={i} 
                          initial={{ scale: 0.8, opacity: 0 }}
                          animate={{ scale: 1, opacity: 1 }}
                          className="aspect-square game-panel bg-black/40 rounded border-game-gold/10 flex items-center justify-center hover:border-game-gold/50 cursor-pointer group"
                        >
                          <Zap className="w-6 h-6 opacity-30 group-hover:opacity-100 transition-opacity" />
                        </motion.div>
                      ))}
                    </div>
                  </div>
                </div>
              </PopupWrapper>
            )}

            {activePopup === 'market' && (
              <PopupWrapper key="market" title="Market" onClose={() => setActivePopup(null)}>
                <div className="space-y-6">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left opacity-50 uppercase text-[10px]">
                        <th className="pb-2">Name</th>
                        <th className="pb-2">Price</th>
                        <th className="pb-2">Trend</th>
                        <th className="pb-2 text-right">Action</th>
                      </tr>
                    </thead>
                    <tbody className="space-y-2">
                      {[
                        { name: 'Carrot', icon: <Carrot className="w-4 h-4 text-orange-500" />, price: '$120', trend: '+$20' },
                        { name: 'Pumpkin', icon: <Apple className="w-4 h-4 text-red-500" />, price: '$130', trend: '+$30' },
                        { name: 'Grape', icon: <Grape className="w-4 h-4 text-purple-500" />, price: '$100', trend: '-$10' },
                      ].map(item => (
                        <tr key={item.name} className="border-t border-white/5">
                          <td className="py-3 flex items-center gap-2 font-bold">{item.icon} {item.name}</td>
                          <td className="py-3 gold-text">{item.price}</td>
                          <td className={cn("py-3 text-xs", item.trend.startsWith('+') ? "text-green-400" : "text-red-400")}>{item.trend}</td>
                          <td className="py-3 text-right">
                            <button className="game-button px-3 py-1 rounded text-[10px] uppercase font-bold">Sell</button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>

                  <div className="h-48 w-full bg-black/40 rounded-lg p-2 border border-white/5">
                    <ResponsiveContainer width="100%" height="100%">
                      <LineChart data={MARKET_DATA}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#333" />
                        <XAxis dataKey="time" stroke="#666" fontSize={10} />
                        <YAxis stroke="#666" fontSize={10} />
                        <Tooltip 
                          contentStyle={{ backgroundColor: '#1a1a1a', border: '1px solid #8b4513' }}
                          itemStyle={{ color: '#ffd700' }}
                        />
                        <Line type="monotone" dataKey="price" stroke="#ffd700" strokeWidth={2} dot={{ fill: '#ffd700' }} />
                      </LineChart>
                    </ResponsiveContainer>
                  </div>
                </div>
              </PopupWrapper>
            )}

            {activePopup === 'farm' && (
              <PopupWrapper key="farm" title="Farm Management" onClose={() => setActivePopup(null)}>
                <div className="space-y-6">
                  <div className="flex items-center justify-between bg-black/40 p-3 rounded-lg border border-game-gold/20">
                    <div className="flex items-center gap-2">
                      <div className="w-8 h-8 bg-amber-900/40 rounded-full flex items-center justify-center border border-game-gold/30">
                        <div className="w-3 h-3 bg-amber-600 rounded-full" />
                      </div>
                      <span className="text-sm font-bold uppercase gold-text">Total Seeds:</span>
                    </div>
                    <span className="text-xl font-black text-game-gold">{totalSeeds}</span>
                  </div>

                  <div className="grid grid-cols-3 gap-4">
                    {[
                      { id: 'carrots', label: 'Carrot', icon: <Carrot className="w-10 h-10 text-orange-500" />, color: 'text-orange-500' },
                      { id: 'pumpkins', label: 'Pumpkin', icon: <Apple className="w-10 h-10 text-red-500" />, color: 'text-red-500' },
                      { id: 'grapes', label: 'Grape', icon: <Grape className="w-10 h-10 text-purple-500" />, color: 'text-purple-500' },
                    ].map(item => (
                      <motion.button 
                        key={item.id}
                        whileTap={{ scale: 0.95 }}
                        onClick={() => plantCrop(item.id as any)}
                        disabled={totalSeeds <= 0}
                        className={cn(
                          "game-panel p-3 bg-black/40 border-game-gold/20 rounded-lg flex flex-col items-center gap-2 transition-all hover:border-game-gold/60",
                          totalSeeds <= 0 && "opacity-50 grayscale cursor-not-allowed"
                        )}
                      >
                        <div className="text-[10px] font-bold uppercase gold-text">{item.label}</div>
                        <div className="w-16 h-16 bg-game-wood rounded flex items-center justify-center border border-game-gold/30 shadow-inner relative">
                          {item.icon}
                          {crops[item.id as keyof typeof crops].growing > 0 && (
                            <div className="absolute -top-1 -right-1 bg-green-600 text-[8px] px-1 rounded border border-white/20 font-bold animate-pulse">
                              {crops[item.id as keyof typeof crops].growing}
                            </div>
                          )}
                        </div>
                        <div className={cn(
                          "text-[8px] font-bold uppercase",
                          crops[item.id as keyof typeof crops].growing > 0 ? "text-green-400" : "opacity-50"
                        )}>
                          {crops[item.id as keyof typeof crops].growing > 0 
                            ? `${crops[item.id as keyof typeof crops].growing} Growing` 
                            : 'Click to grow'}
                        </div>
                      </motion.button>
                    ))}
                  </div>

                  <div className="game-panel p-4 bg-black/60 rounded-xl border-game-gold/30">
                    <h3 className="text-xs font-bold uppercase gold-text mb-3">Nông Sản</h3>
                    <div className="space-y-2 text-sm">
                      <div className="flex justify-between items-center">
                        <div className="flex items-center gap-2"><Carrot className="w-4 h-4 text-orange-500" /> Carrots:</div>
                        <div className="font-bold text-game-gold">{crops.carrots.harvested}</div>
                      </div>
                      <div className="flex justify-between items-center">
                        <div className="flex items-center gap-2"><Apple className="w-4 h-4 text-red-500" /> Pumpkins:</div>
                        <div className="font-bold text-game-gold">{crops.pumpkins.harvested}</div>
                      </div>
                      <div className="flex justify-between items-center">
                        <div className="flex items-center gap-2"><Grape className="w-4 h-4 text-purple-500" /> Grapes:</div>
                        <div className="font-bold text-game-gold">{crops.grapes.harvested}</div>
                      </div>
                    </div>
                  </div>
                </div>
              </PopupWrapper>
            )}

            {activePopup === 'guide' && (
              <PopupWrapper key="guide" title="Game Guide" onClose={() => { setActivePopup(null); setSelectedTower(null); setSelectedItem(null); }}>
                <div className="space-y-6">
                  {/* Tabs */}
                  <div className="flex gap-1 bg-black/40 p-1 rounded-lg border border-white/5">
                    {(['howToPlay', 'shopInfo', 'towerTiers'] as GuideTab[]).map(tab => (
                      <button
                        key={tab}
                        onClick={() => { setGuideTab(tab); setSelectedTower(null); setSelectedItem(null); }}
                        className={cn(
                          "flex-1 py-1.5 rounded text-[10px] font-bold uppercase transition-all",
                          guideTab === tab ? "bg-game-gold text-black" : "hover:bg-white/5 opacity-60"
                        )}
                      >
                        {tab === 'howToPlay' && 'How to Play'}
                        {tab === 'shopInfo' && 'Shop Info'}
                        {tab === 'towerTiers' && 'Tower Tiers'}
                      </button>
                    ))}
                  </div>

                  <div className="min-h-[350px]">
                    {guideTab === 'howToPlay' && (
                      <motion.div 
                        initial={{ opacity: 0, y: 10 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="space-y-4"
                      >
                        <div className="game-panel p-4 bg-black/40 rounded-lg border-game-gold/20">
                          <h3 className="text-sm font-bold gold-text uppercase mb-2 flex items-center gap-2">
                            <Zap className="w-4 h-4" /> Gameplay Basics
                          </h3>
                          <ul className="text-xs space-y-2 opacity-80 list-disc pl-4">
                            <li>Plant seeds in the <span className="text-orange-400 font-bold">Farm</span> to grow crops.</li>
                            <li>Harvest crops and sell them in the <span className="text-purple-400 font-bold">Market</span> for Gold.</li>
                            <li>Use Gold to buy <span className="text-game-gold font-bold">Tower Packs</span> and equipment in the Shop.</li>
                            <li>Defend your farm from waves of enemies using your towers.</li>
                          </ul>
                        </div>
                        <div className="game-panel p-4 bg-black/40 rounded-lg border-game-gold/20">
                          <h3 className="text-sm font-bold gold-text uppercase mb-2 flex items-center gap-2">
                            <Shield className="w-4 h-4" /> Strategy
                          </h3>
                          <p className="text-xs opacity-80 leading-relaxed">
                            Combine different tower types to cover each other's weaknesses. Use <span className="text-red-400 font-bold">Weapons</span> to boost damage and <span className="text-blue-400 font-bold">Buffs</span> to gain tactical advantages.
                          </p>
                        </div>
                      </motion.div>
                    )}

                    {guideTab === 'shopInfo' && (
                      <div className="flex flex-col gap-4 h-[420px]">
                        {/* Sub-tabs for Shop Info */}
                        <div className="flex gap-2 border-b border-white/5 pb-2">
                          {(['weapons', 'buffs'] as ShopInfoTab[]).map(tab => (
                            <button
                              key={tab}
                              onClick={() => { setShopInfoTab(tab); setSelectedItem(null); }}
                              className={cn(
                                "px-4 py-1 rounded-full text-[9px] font-bold uppercase transition-all flex items-center gap-1.5",
                                shopInfoTab === tab 
                                  ? "bg-game-gold/20 text-game-gold border border-game-gold/50" 
                                  : "bg-white/5 text-white/40 border border-transparent hover:bg-white/10"
                              )}
                            >
                              {tab === 'weapons' ? <Swords className="w-3 h-3" /> : <Zap className="w-3 h-3" />}
                              {tab}
                            </button>
                          ))}
                        </div>

                        <div className="flex gap-4 flex-1 min-h-0">
                          {/* Left Side: Scrollable Grid */}
                          <div className="w-1/2 flex flex-col gap-3">
                            <div className="flex-1 overflow-y-auto pr-2 custom-scrollbar">
                              <div className="grid grid-cols-4 gap-2">
                                {(shopInfoTab === 'weapons' ? WEAPONS_DATA : BUFFS_DATA).map(item => (
                                  <button
                                    key={item.id}
                                    onClick={() => setSelectedItem(item)}
                                    className={cn(
                                      "aspect-square game-panel bg-black/40 border-game-gold/10 rounded-lg flex items-center justify-center hover:border-game-gold/50 transition-all group overflow-hidden relative",
                                      selectedItem?.id === item.id && "border-game-gold ring-2 ring-game-gold/30"
                                    )}
                                  >
                                    <img 
                                      src={item.image} 
                                      alt={item.name} 
                                      className={cn(
                                        "w-full h-full object-cover transition-all duration-500",
                                        selectedItem?.id === item.id ? "opacity-100 scale-110" : "opacity-40 group-hover:opacity-80"
                                      )} 
                                      referrerPolicy="no-referrer" 
                                    />
                                    {selectedItem?.id === item.id && (
                                      <div className="absolute inset-0 bg-game-gold/10 pointer-events-none" />
                                    )}
                                  </button>
                                ))}
                              </div>
                            </div>
                            
                            <div className="game-panel p-2.5 bg-black/60 rounded-lg border-game-gold/20 flex justify-between items-center">
                              <div className="flex flex-col">
                                <span className="text-[7px] opacity-40 uppercase font-bold leading-none mb-1">Market Status</span>
                                <span className="text-[9px] text-game-gold font-black uppercase">Active Supply</span>
                              </div>
                              <div className="flex gap-3">
                                <div className="text-right">
                                  <div className="text-[7px] opacity-40 uppercase font-bold">Reroll</div>
                                  <div className="text-[10px] font-bold">5G</div>
                                </div>
                                <div className="text-right">
                                  <div className="text-[7px] opacity-40 uppercase font-bold">Limit</div>
                                  <div className="text-[10px] font-bold">4/R</div>
                                </div>
                              </div>
                            </div>
                          </div>

                          {/* Right Side: Refined Detail Panel */}
                          <div className="w-1/2 game-panel bg-black/80 rounded-xl border-game-gold/40 p-0 flex flex-col relative overflow-hidden shadow-2xl">
                            <AnimatePresence mode="wait">
                              {selectedItem ? (
                                <motion.div 
                                  key={selectedItem.id}
                                  initial={{ opacity: 0, y: 10 }}
                                  animate={{ opacity: 1, y: 0 }}
                                  exit={{ opacity: 0, y: -10 }}
                                  className="w-full h-full flex flex-col"
                                >
                                  {/* Item Header Section */}
                                  <div className="p-4 border-b border-game-gold/20 bg-game-gold/5 flex items-center gap-4">
                                    <div className="w-16 h-16 bg-game-wood rounded-lg border border-game-gold/30 overflow-hidden shadow-lg shrink-0">
                                      <img 
                                        src={selectedItem.image} 
                                        alt={selectedItem.name} 
                                        className="w-full h-full object-cover"
                                        referrerPolicy="no-referrer"
                                      />
                                    </div>
                                    <div className="flex-1 min-w-0">
                                      <div className="text-[7px] font-black text-game-gold/50 uppercase tracking-[0.2em] mb-1">Data Log #{selectedItem.id.toUpperCase()}</div>
                                      <h3 className="text-xl font-black gold-text uppercase leading-tight truncate">
                                        {selectedItem.name}
                                      </h3>
                                      <div className="flex items-center gap-1.5 mt-1">
                                        <div className="w-1 h-1 rounded-full bg-game-gold animate-pulse" />
                                        <span className="text-[8px] font-bold text-white/40 uppercase tracking-widest">
                                          {shopInfoTab === 'weapons' ? 'Weaponry Class' : 'Tactical Enhancement'}
                                        </span>
                                      </div>
                                    </div>
                                  </div>
                                  
                                  <div className="p-4 space-y-4 flex-1 flex flex-col">
                                    {/* Effect Box - Hardware Style */}
                                    <div className="relative group">
                                      <div className="absolute -inset-0.5 bg-game-gold/20 rounded blur opacity-20 group-hover:opacity-40 transition duration-500" />
                                      <div className="relative bg-black/40 border border-game-gold/30 p-3 rounded-lg overflow-hidden">
                                        <div className="absolute top-0 right-0 p-1">
                                          <Zap className="w-2.5 h-2.5 text-game-gold/30" />
                                        </div>
                                        <div className="text-[8px] opacity-40 uppercase font-black tracking-tighter mb-1">Primary Output</div>
                                        <div className="text-sm font-black text-game-gold flex items-center gap-2">
                                          <ChevronRight className="w-3 h-3" />
                                          {'stats' in selectedItem ? selectedItem.stats : selectedItem.effect}
                                        </div>
                                      </div>
                                    </div>

                                    {/* Description / Lore */}
                                    <div className="flex-1">
                                      <div className="flex items-center gap-2 mb-2">
                                        <div className="w-1 h-3 bg-game-gold/40" />
                                        <span className="text-[8px] opacity-40 uppercase font-black tracking-widest">Intelligence Report</span>
                                      </div>
                                      <p className="text-[10px] text-white/70 leading-relaxed font-medium italic pl-3 border-l border-white/5">
                                        "{selectedItem.description}"
                                      </p>
                                    </div>

                                    {/* Footer Micro-details */}
                                    <div className="pt-3 border-t border-white/5 flex justify-between items-end">
                                      <div className="space-y-1">
                                        <div className="flex gap-1">
                                          {[1, 2, 3, 4, 5].map(i => (
                                            <div key={i} className={cn("w-1 h-1 rounded-full", i <= 3 ? "bg-game-gold" : "bg-white/10")} />
                                          ))}
                                        </div>
                                        <div className="text-[6px] opacity-30 uppercase font-bold">Stability Index</div>
                                      </div>
                                      <div className="text-right">
                                        <div className="text-[10px] font-black text-game-gold/80">MK-IV</div>
                                        <div className="text-[6px] opacity-30 uppercase font-bold">Revision</div>
                                      </div>
                                    </div>
                                  </div>
                                </motion.div>
                              ) : (
                                <motion.div 
                                  initial={{ opacity: 0 }}
                                  animate={{ opacity: 1 }}
                                  className="text-center space-y-4 p-8"
                                >
                                  <div className="relative w-16 h-16 mx-auto">
                                    <div className="absolute inset-0 border-2 border-game-gold/10 rounded-full animate-ping" />
                                    <div className="relative w-full h-full rounded-full border border-game-gold/20 flex items-center justify-center bg-game-gold/5">
                                      <Info className="w-8 h-8 text-game-gold/20" />
                                    </div>
                                  </div>
                                  <div className="space-y-1">
                                    <p className="text-[10px] text-game-gold/40 uppercase font-black tracking-[0.3em]">Awaiting Input</p>
                                    <p className="text-[8px] opacity-20 uppercase font-bold">Select an asset from the database</p>
                                  </div>
                                </motion.div>
                              )}
                            </AnimatePresence>
                          </div>
                        </div>
                      </div>
                    )}

                    {guideTab === 'towerTiers' && (
                      <div className="space-y-6">
                        {selectedTower ? (
                          <motion.div 
                            initial={{ opacity: 0, x: 20 }}
                            animate={{ opacity: 1, x: 0 }}
                            className="space-y-4"
                          >
                            <button 
                              onClick={() => setSelectedTower(null)}
                              className="text-[10px] uppercase font-bold text-game-gold flex items-center gap-1 hover:underline"
                            >
                              <ChevronRight className="w-3 h-3 rotate-180" /> Back to list
                            </button>
                            
                            <div className="flex gap-4">
                              <div className="w-1/3 aspect-square game-panel bg-black/40 rounded-lg overflow-hidden border-game-gold/30">
                                <img 
                                  src={selectedTower.image} 
                                  alt={selectedTower.name} 
                                  className="w-full h-full object-cover"
                                  referrerPolicy="no-referrer"
                                />
                              </div>
                              <div className="flex-1 space-y-2">
                                <h3 className="text-xl font-bold gold-text uppercase">{selectedTower.name}</h3>
                                <div className="inline-block px-2 py-0.5 bg-game-gold/20 border border-game-gold/50 rounded text-[10px] font-bold text-game-gold uppercase">
                                  Tier {selectedTower.tier}
                                </div>
                                
                                <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-[11px]">
                                  <div className="flex justify-between border-b border-white/5 pb-0.5">
                                    <span className="opacity-50">Health:</span>
                                    <span className="font-bold">{selectedTower.stats.health}</span>
                                  </div>
                                  <div className="flex justify-between border-b border-white/5 pb-0.5">
                                    <span className="opacity-50">Damage:</span>
                                    <span className="font-bold text-red-400">{selectedTower.stats.damage}</span>
                                  </div>
                                  <div className="flex justify-between border-b border-white/5 pb-0.5">
                                    <span className="opacity-50">Atk Speed:</span>
                                    <span className="font-bold">{selectedTower.stats.attackSpeed}</span>
                                  </div>
                                  <div className="flex justify-between border-b border-white/5 pb-0.5">
                                    <span className="opacity-50">Move Speed:</span>
                                    <span className="font-bold">{selectedTower.stats.moveSpeed}</span>
                                  </div>
                                  <div className="flex justify-between border-b border-white/5 pb-0.5">
                                    <span className="opacity-50">Dmg Type:</span>
                                    <span className="font-bold text-orange-400">{selectedTower.stats.damageType}</span>
                                  </div>
                                  <div className="flex justify-between border-b border-white/5 pb-0.5">
                                    <span className="opacity-50">Armor:</span>
                                    <span className="font-bold text-blue-400">{selectedTower.stats.armor}</span>
                                  </div>
                                  <div className="flex justify-between border-b border-white/5 pb-0.5 col-span-2">
                                    <span className="opacity-50">Armor Type:</span>
                                    <span className="font-bold flex items-center gap-1">
                                      <Shield className="w-3 h-3" /> {selectedTower.stats.armorType}
                                    </span>
                                  </div>
                                </div>
                              </div>
                            </div>

                            <div className="game-panel p-3 bg-black/60 rounded-lg border-game-gold/20">
                              <div className="flex items-center gap-2 mb-2">
                                <div className="w-8 h-8 bg-game-wood rounded border border-game-gold/30 flex items-center justify-center text-game-gold">
                                  {selectedTower.skill.icon}
                                </div>
                                <div>
                                  <div className="text-xs font-bold uppercase gold-text">{selectedTower.skill.name}</div>
                                  <div className="text-[9px] opacity-50 uppercase tracking-tighter">Special Skill</div>
                                </div>
                              </div>
                              <p className="text-xs opacity-80 leading-relaxed italic">
                                "{selectedTower.skill.description}"
                              </p>
                            </div>
                          </motion.div>
                        ) : (
                          <div className="space-y-8">
                            {Object.entries(TOWERS_DATA).sort((a, b) => Number(b[0]) - Number(a[0])).map(([tier, towers]) => (
                              <div key={tier} className="space-y-3">
                                <h3 className="text-sm font-bold uppercase gold-text border-b border-game-border pb-1 flex items-center gap-2">
                                  <span className="w-1.5 h-1.5 bg-game-gold rounded-full" />
                                  Tier {tier}
                                </h3>
                                <div className="grid grid-cols-3 gap-3">
                                  {towers.map(tower => (
                                    <motion.button
                                      key={tower.id}
                                      whileHover={{ scale: 1.05, y: -2 }}
                                      whileTap={{ scale: 0.95 }}
                                      onClick={() => setSelectedTower(tower)}
                                      className="game-panel p-2 bg-black/40 border-game-gold/10 rounded-lg flex flex-col items-center gap-2 hover:border-game-gold/50 transition-all group"
                                    >
                                      <div className="w-full aspect-square bg-game-wood rounded border border-game-gold/20 overflow-hidden">
                                        <img 
                                          src={tower.image} 
                                          alt={tower.name} 
                                          className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"
                                          referrerPolicy="no-referrer"
                                        />
                                      </div>
                                      <div className="text-[10px] font-bold uppercase text-center leading-tight group-hover:text-game-gold transition-colors">
                                        {tower.name}
                                      </div>
                                    </motion.button>
                                  ))}
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </PopupWrapper>
            )}

            {activePopup === 'menu' && (
              <div className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 pointer-events-auto">
                <motion.div 
                  initial={{ scale: 0.8, opacity: 0 }}
                  animate={{ scale: 1, opacity: 1 }}
                  className="game-panel w-80 p-8 flex flex-col gap-4 bg-game-wood/90"
                >
                  <h2 className="text-3xl font-black text-center gold-text uppercase italic mb-4">Game Menu</h2>
                  <button className="game-button py-3 rounded-lg font-bold uppercase tracking-widest text-lg">Settings</button>
                  <button className="game-button py-3 rounded-lg font-bold uppercase tracking-widest text-lg">Save Game</button>
                  <button className="game-button py-3 rounded-lg font-bold uppercase tracking-widest text-lg">Load Game</button>
                  <div className="h-px bg-game-gold/20 my-2" />
                  <button 
                    onClick={() => setActivePopup(null)}
                    className="game-button py-3 rounded-lg font-bold uppercase tracking-widest text-lg text-red-400"
                  >
                    Back to Game
                  </button>
                </motion.div>
              </div>
            )}
          </AnimatePresence>
        </div>
      </div>

      {/* Bottom Bar - Card Deck */}
      <div className="relative z-10 p-4 bg-gradient-to-t from-black/80 to-transparent flex justify-center items-end gap-2">
        {CARDS.map((card, idx) => (
          <motion.div 
            key={card.id}
            whileHover={{ y: -20, scale: 1.1 }}
            className={cn(
              "w-24 aspect-[2/3] game-panel rounded-lg p-2 flex flex-col items-center justify-between cursor-pointer group",
              card.color
            )}
          >
            <div className="w-full flex justify-between items-start">
               <div className="w-5 h-5 bg-black/40 rounded flex items-center justify-center text-[10px] font-bold border border-white/20">
                  {card.level}
               </div>
               <div className="w-4 h-4 opacity-50">
                  {card.type === 'tower' && <Shield className="w-full h-full" />}
                  {card.type === 'weapon' && <Swords className="w-full h-full" />}
                  {card.type === 'buff' && <Zap className="w-full h-full" />}
               </div>
            </div>
            
            <div className="flex-1 flex items-center justify-center">
               <div className="w-12 h-12 bg-black/20 rounded-full flex items-center justify-center border border-white/10 group-hover:border-game-gold/50 transition-colors">
                  {card.type === 'tower' && <Shield className="w-8 h-8 text-white/80" />}
                  {card.type === 'weapon' && <Swords className="w-8 h-8 text-white/80" />}
                  {card.type === 'buff' && <Zap className="w-8 h-8 text-white/80" />}
               </div>
            </div>

            <div className="w-full text-center">
               <div className="text-[8px] font-bold uppercase leading-tight">{card.name}</div>
               <div className="text-[6px] opacity-50 uppercase">Level {card.level}</div>
            </div>
          </motion.div>
        ))}
      </div>
    </div>
  );
}
