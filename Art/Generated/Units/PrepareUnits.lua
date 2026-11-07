-- Export a shared logical pixel grid at 4x, for Unity PPU 128 (32 logical px/unit).
-- Original canvases are preserved; only resize and placement are applied, never trim.
local root="C:/Work/jaebyProj/"
local pc=app.pixelColor
local ink={19,16,21}
local dark={43,34,39}
local cream={255,242,219}
local palettes={
 Slime={ink,dark,{70,31,66},{141,62,116},{215,136,168},{255,201,191}},
 StoneSpirit={ink,dark,{66,48,76},{112,94,122},{159,140,162},{232,186,151},cream},
 ThornSlime={ink,dark,{44,61,42},{76,104,49},{140,170,70},{201,219,126},{143,94,76},{232,217,195},cream},
 SporeSlime={ink,dark,{73,29,40},{151,53,60},{221,149,129},{143,94,45},{232,170,69},{255,217,140},cream},
 BellSlime={ink,dark,{92,63,53},{158,112,71},{232,186,100},{143,94,45},{232,170,50},{255,220,111},{71,94,76},{126,145,100},cream},
 IronPebble={ink,dark,{47,48,68},{75,88,118},{132,151,177},{102,86,115},{173,159,177},{232,186,80},cream},
 RiftSpirit={ink,dark,{66,31,82},{124,55,133},{179,96,170},{244,178,183},{49,141,157},{102,226,215},cream},
 ManaFirefly={ink,dark,{89,45,45},{143,94,76},{221,149,129},{66,101,96},{126,182,153},{197,230,177},{143,94,45},{232,170,50},{255,220,111},cream},
 SproutSpirit={ink,dark,{81,43,49},{143,73,64},{195,122,82},{244,186,122},{44,78,58},{85,146,83},{162,205,120},cream},
 ShadeMushroom={ink,dark,{63,29,83},{111,49,134},{168,93,180},{221,173,232},{96,79,107},{155,137,166},{211,195,208},cream},
 VoidMoth={ink,dark,{38,24,63},{57,40,91},{105,65,121},{159,81,139},{198,50,121},{238,112,157},cream},
 CrystalHedgehog={ink,dark,{78,43,50},{143,73,64},{195,122,101},{244,186,151},{80,37,88},{143,61,139},{212,111,172},{41,102,114},{80,161,168},{158,229,213},cream},
 MeadowHeart={ink,dark,{66,48,76},{112,94,122},{180,145,151},{232,186,151},{44,61,42},{76,104,49},{140,170,70},{143,73,64},{195,122,82},{112,18,56},{210,37,86},{255,117,151},cream},
 RiftWarden={ink,dark,{66,31,82},{109,73,127},{159,107,167},{244,186,183},{49,141,157},{102,226,215},cream},
 VoidHeadmaster={ink,dark,{59,24,58},{105,47,95},{158,82,133},{110,70,45},{195,140,67},{244,186,122},{143,94,76},{232,217,195},{198,50,121},{238,112,157},cream}
}
local names={"Slime","StoneSpirit","ThornSlime","SporeSlime","BellSlime","IronPebble","RiftSpirit","ManaFirefly","SproutSpirit","ShadeMushroom","VoidMoth","CrystalHedgehog","MeadowHeart","RiftWarden","VoidHeadmaster"}
local heights={30,34,37,36,42,34,40,36,48,40,38,41,72,78,74}
local function bounds(img,threshold)
 local x0,y0,x1,y1=img.width,img.height,-1,-1
 for it in img:pixels() do
  if pc.rgbaA(it())>=threshold then
   x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
  end
 end
 assert(x1>=x0,"Empty sprite")
 return x0,y0,x1,y1
end
local function nearest(c,palette)
 local best,dist=palette[1],1e20
 local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
 for _,p in ipairs(palette) do
  local d=(r-p[1])^2+(g-p[2])^2+(b-p[3])^2
  if d<dist then best=p;dist=d end
 end
 return pc.rgba(best[1],best[2],best[3],255)
end
local log=io.open(root.."Art/Generated/Units/Import.csv","w")
log:write("Name,LogicalWidth,LogicalHeight,Width,Height,PixelsPerUnit,Foot,VisibleHeight\n")
local function export(sprite,name)
 local img=sprite.cels[1].image
 local x0,y0,x1,y1=bounds(img,1)
 log:write(name..","..sprite.width..","..sprite.height..","..sprite.width*4 ..","..sprite.height*4 ..",128,"..(sprite.height-1-y1)*4 ..","..(y1-y0+1)*4 .."\n")
 sprite:saveAs(root.."Art/Generated/Units/"..name..".aseprite")
 sprite:saveAs(root.."Art/Generated/Units/"..name.."Logical.png")
 sprite:resize(sprite.width*4,sprite.height*4)
 sprite:saveAs(root.."Assets/02.Sprites/Units/"..name..".png")
 sprite:close()
end
-- This editable source remains at 64x96, preventing repeated 4x enlargement.
local hero=app.open(root.."Art/Generated/Units/PlayerSwordswoman.aseprite")
assert(hero.width==64 and hero.height==96,"Player source must stay at the logical size")
export(hero,"PlayerSwordswoman")
for index,name in ipairs(names) do
 local source=app.open(root.."Art/Generated/Enemies/"..name.."Original.png")
 local x0,y0,x1,y1=bounds(source.cels[1].image,210)
 local size=index>12 and 96 or 64
 local scale=math.min((index>12 and 70 or 48)/(x1-x0+1),(heights[index]-2)/(y1-y0+1))
 source:resize(math.max(1,math.floor(source.width*scale+.5)),math.max(1,math.floor(source.height*scale+.5)))
 local reduced=source.cels[1].image
 for it in reduced:pixels() do
  local c=it()
  it(pc.rgbaA(c)<210 and 0 or nearest(c,palettes[name]))
 end
 x0,y0,x1,y1=bounds(reduced,1)
 local image=Image(size,size,ColorMode.RGB)
 image:drawImage(reduced,Point(math.floor((size-1)/2-(x0+x1)/2),size-16-y1))
 source:close()
 -- A one-cell ink edge unifies silhouettes and removes soft alpha from generated art.
 local outlined=Image(size,size,ColorMode.RGB)
 local edge=pc.rgba(ink[1],ink[2],ink[3],255)
 for y=0,size-1 do for x=0,size-1 do
  if pc.rgbaA(image:getPixel(x,y))>0 then
   for _,o in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do
    local xx,yy=x+o[1],y+o[2]
    if xx>=0 and yy>=0 and xx<size and yy<size then outlined:drawPixel(xx,yy,edge) end
   end
  end
 end end
 outlined:drawImage(image,Point(0,0))
 local sprite=Sprite(size,size,ColorMode.RGB)
 sprite.cels[1].image=outlined
 export(sprite,name)
end
log:close()
