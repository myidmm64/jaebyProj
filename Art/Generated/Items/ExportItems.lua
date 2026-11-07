local root="C:/Work/jaebyProj/"
local pc=app.pixelColor
local palette={
 Ink={33,24,35},Paper={247,232,200},Shade={154,134,114},Steel={114,137,150},SteelShade={71,80,94},
 Red={195,95,103},RedShade={118,60,78},Mint={122,182,158},MintShade={66,109,101},Gold={235,191,106},GoldShade={161,109,60},
 Purple={164,116,173},PurpleShade={92,64,114}
}
local function color(name) local p=palette[name];return pc.rgba(p[1],p[2],p[3],255) end
local img
local function pixel(x,y,c) if x>=0 and y>=0 and x<32 and y<32 then img:drawPixel(x,y,color(c)) end end
local function rect(x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do pixel(xx,yy,c) end end end
local function line(x0,y0,x1,y1,c,width)
 local dx,dy=math.abs(x1-x0),-math.abs(y1-y0);local sx=x0<x1 and 1 or -1;local sy=y0<y1 and 1 or -1;local err=dx+dy
 while true do rect(x0,y0,width or 1,width or 1,c);if x0==x1 and y0==y1 then break end
  local e=2*err;if e>=dy then err=err+dy;x0=x0+sx end;if e<=dx then err=err+dx;y0=y0+sy end end
end
local function oval(cx,cy,rx,ry,c)
 for y=math.floor(cy-ry),math.ceil(cy+ry) do for x=math.floor(cx-rx),math.ceil(cx+rx) do
  if ((x-cx)/rx)^2+((y-cy)/ry)^2<=1 then pixel(x,y,c) end
 end end
end
local function poly(points,c)
 for y=0,31 do
  local xs={}
  for i=1,#points do local a,b=points[i],points[i%#points+1]
   if (a[2]<=y and b[2]>y) or (b[2]<=y and a[2]>y) then xs[#xs+1]=a[1]+(y-a[2])*(b[1]-a[1])/(b[2]-a[2]) end
  end
  table.sort(xs)
  for i=1,#xs-1,2 do for x=math.ceil(xs[i]),math.floor(xs[i+1]) do pixel(x,y,c) end end
 end
end
local names={"Whetstone","OpeningWeight","TwinBlade","BossLens","PiercingNeedle","WarHorn","Incense","LuckyDie","RiftHammer","Hourglass","OrderSheet","MerchantScale","CoinPurse","RerollTicket","EchoBottle"}
for id,name in ipairs(names) do
 img=Image(32,32,ColorMode.RGB)
 if id==1 then
  poly({{5,12},{21,7},{27,14},{11,20}},"Steel");poly({{5,12},{11,20},{11,24},{5,17}},"SteelShade");poly({{11,20},{27,14},{27,18},{11,24}},"SteelShade")
  line(10,13,19,10,"Paper",2);line(16,16,23,13,"Red",1)
 elseif id==2 then
  oval(16,8,5,5,"Gold");oval(16,8,2,2,"Ink");poly({{9,11},{23,11},{28,26},{4,26}},"SteelShade");rect(10,13,12,9,"Steel");rect(13,15,6,5,"Mint")
 elseif id==3 then
  line(7,25,25,7,"Ink",4);line(8,24,25,7,"Paper",2);line(6,21,10,25,"Gold",2);line(5,26,8,23,"Red",2)
  line(7,7,24,24,"Ink",4);line(8,7,25,24,"Steel",2);line(21,21,25,25,"Gold",2);line(24,24,27,27,"RedShade",2)
 elseif id==4 then
  oval(14,13,9,9,"RedShade");oval(14,13,7,7,"Red");oval(14,13,5,5,"PurpleShade");line(10,12,14,8,"Paper",2)
  line(20,20,26,26,"GoldShade",5);line(20,20,26,26,"Gold",2)
 elseif id==5 then
  poly({{4,27},{13,9},{22,4},{26,8},{20,17}},"Steel");line(5,26,21,9,"Paper",1);oval(23,7,4,4,"Gold");oval(23,7,2,2,"Ink")
 elseif id==6 then
  poly({{4,12},{4,22},{12,18},{25,18},{25,12},{12,14}},"GoldShade");rect(12,13,13,3,"Gold")
  poly({{25,8},{29,5},{29,25},{25,22}},"Gold");line(11,19,14,24,"Gold",3);line(14,24,21,24,"Gold",3);line(21,24,23,19,"Gold",3)
 elseif id==7 then
  poly({{7,17},{25,17},{22,27},{10,27}},"GoldShade");rect(8,17,17,3,"Gold");rect(11,20,11,4,"MintShade");line(16,16,16,11,"Red",2)
  line(17,8,20,5,"Shade",2);line(20,5,18,2,"Shade",2);line(12,12,10,9,"Paper",2)
 elseif id==8 then
  poly({{7,5},{24,5},{27,9},{27,25},{7,27},{4,23},{4,8}},"Shade");rect(5,6,18,18,"Paper");rect(23,9,3,15,"MintShade")
  rect(8,9,3,3,"Ink");rect(18,9,3,3,"Ink");rect(13,14,3,3,"RedShade");rect(8,19,3,3,"Ink");rect(18,19,3,3,"Ink")
 elseif id==9 then
  line(11,25,22,9,"GoldShade",5);line(12,25,23,9,"Gold",2);poly({{9,3},{26,8},{23,17},{6,12}},"PurpleShade");poly({{10,4},{24,8},{22,12},{8,8}},"Purple");line(18,6,14,11,"Paper",2)
 elseif id==10 then
  rect(7,4,18,3,"Gold");rect(7,26,18,3,"Gold");line(10,8,22,24,"Steel",1);line(22,8,10,24,"Steel",1)
  poly({{11,8},{21,8},{16,15}},"Paper");poly({{10,24},{22,24},{17,18},{15,18}},"GoldShade");line(16,14,16,20,"Gold",1)
 elseif id==11 then
  poly({{7,4},{21,4},{26,9},{26,27},{7,27}},"Shade");poly({{8,5},{20,5},{20,10},{25,10},{25,26},{8,26}},"Paper")
  rect(11,9,2,2,"Red");rect(16,9,3,2,"GoldShade");rect(11,15,2,2,"Red");rect(16,15,6,2,"GoldShade");line(10,22,13,24,"MintShade",2);line(13,24,18,20,"MintShade",2)
 elseif id==12 then
  line(16,7,16,25,"GoldShade",3);rect(10,26,14,2,"Gold");line(5,10,27,10,"Gold",2);oval(17,7,3,3,"Red");line(7,11,5,19,"GoldShade",1);line(7,11,10,19,"GoldShade",1)
  line(25,11,22,19,"GoldShade",1);line(25,11,28,19,"GoldShade",1);poly({{3,19},{12,19},{10,23},{5,23}},"Mint");poly({{20,19},{30,19},{27,23},{23,23}},"Mint")
 elseif id==13 then
  poly({{9,5},{23,5},{20,11},{26,18},{25,26},{7,26},{5,18},{12,11}},"GoldShade");poly({{10,12},{20,12},{24,20},{21,25},{8,24},{7,19}},"Shade")
  line(10,10,22,10,"MintShade",3);oval(18,20,6,6,"Gold");oval(18,20,3,3,"GoldShade");line(18,17,18,22,"Paper",1)
 elseif id==14 then
  poly({{4,7},{27,7},{27,12},{25,14},{27,16},{27,24},{4,24},{4,18},{6,16},{4,14}},"GoldShade");rect(6,9,19,13,"Paper");oval(16,15,6,5,"MintShade");oval(16,15,3,2,"Paper");poly({{19,11},{24,11},{22,16}},"MintShade")
 elseif id==15 then
  oval(16,21,9,8,"MintShade");oval(15,20,7,6,"PurpleShade");rect(12,7,8,9,"Mint");rect(11,5,10,4,"GoldShade");rect(12,5,8,2,"Gold");line(10,18,12,15,"Paper",2);rect(20,19,2,5,"Purple")
 end
 -- Continuous one-cell outline around the whole icon; all colors are opaque.
 local result=Image(32,32,ColorMode.RGB)
 for y=0,31 do for x=0,31 do
  if pc.rgbaA(img:getPixel(x,y))>0 then
   for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do
    local xx,yy=x+d[1],y+d[2]
    if xx>=0 and yy>=0 and xx<32 and yy<32 then result:drawPixel(xx,yy,color("Ink")) end
   end
  end
 end end
 result:drawImage(img,Point(0,0))
 local sprite=Sprite(32,32,ColorMode.RGB);sprite.cels[1].image=result
 sprite:saveAs(root.."Art/Generated/Items/"..name..".aseprite")
 sprite:resize(128,128);sprite:saveAs(root.."Assets/02.Sprites/UI/Items/"..name..".png");sprite:close()
end
