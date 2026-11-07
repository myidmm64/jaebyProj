local root="C:/Work/jaebyProj/"
local pc=app.pixelColor
local out=root.."Assets/02.Sprites/Units/"
local log=io.open(root.."Art/Generated/Units/Import.csv","w")
log:write("Name,Width,Height,Foot,VisibleHeight\n")
local function alpha(c) return pc.rgbaA(c) end
local function fit(path,w,h)
  local src=app.open(path)
  local scale=math.min(w/src.width,h/src.height)
  local sw,sh=math.max(1,math.floor(src.width*scale+.5)),math.max(1,math.floor(src.height*scale+.5))
  src:resize(sw,sh)
  local image=Image(w,h,ColorMode.RGB)
  image:drawImage(src.cels[1].image,Point(math.floor((w-sw)/2),math.floor((h-sh)/2)))
  src:close()
  return image
end
local function pixel(img,x,y,c) if x>=0 and y>=0 and x<img.width and y<img.height then img:drawPixel(x,y,c) end end
local function bounds(img)
 local x0,y0,x1,y1=img.width,img.height,-1,-1
 for y=0,img.height-1 do for x=0,img.width-1 do if alpha(img:getPixel(x,y))>0 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y) end end end
 return x0,y0,x1,y1
end
local function save(img,name)
 local sprite=Sprite(img.width,img.height,ColorMode.RGB)
 sprite.cels[1].image=img
 sprite:saveAs(out..name..".png")
 sprite:saveAs(root.."Art/Generated/Units/"..name..".aseprite")
 local x0,y0,x1,y1=bounds(img)
 log:write(name..","..img.width..","..img.height..","..(img.height-1-y1)..","..(y1-y0+1).."\n")
 sprite:close()
end
local palette={
 {19,16,21},{43,34,39},{65,55,63},{91,81,91},{109,67,54},{143,94,76},{177,122,101},
 {94,34,40},{151,53,60},{184,77,82},{221,149,129},{250,187,159},{255,217,187},
 {184,169,151},{232,217,195},{255,242,219},{149,111,45},{232,186,80},{116,32,22}
}
local hero=fit(root.."Art/Generated/Characters/PlayerCasualOriginal.png",64,96)
for it in hero:pixels() do
 local c=it()
 if alpha(c)<200 then it(0) else
  local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
  local best,dist=palette[1],1e20
  for _,p in ipairs(palette) do local d=(r-p[1])^2+(g-p[2])^2+(b-p[3])^2;if d<dist then best=p;dist=d end end
  it(pc.rgba(best[1],best[2],best[3],255))
 end
end
save(hero,"PlayerSwordswoman")
local names={"Slime","StoneSpirit","ThornSlime","SporeSlime","BellSlime","IronPebble","RiftSpirit","ManaFirefly","SproutSpirit","ShadeMushroom","VoidMoth","CrystalHedgehog"}
local bodies={0,1,0,0,0,1,1,1,1,0,1,1}
local colors={{215,112,161},{134,125,153},{138,186,93},{169,104,205},{86,180,203},{98,128,157},{132,100,195},{223,177,73},{95,168,112},{109,81,150},{133,173,219},{175,145,218}}
local ink=pc.rgba(25,21,30,255)
local function rect(img,x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do pixel(img,xx,yy,c) end end end
local function ellipse(img,cx,cy,rx,ry,c)
 for y=math.floor(cy-ry),math.ceil(cy+ry) do for x=math.floor(cx-rx),math.ceil(cx+rx) do
  local d=((x-cx)/rx)^2+((y-cy)/ry)^2
  if d<=1 then pixel(img,x,y,d>.58 and ink or c) end
 end end
end
local function triangle(img,cx,top,half,bottom,c)
 for y=top,bottom do local span=math.floor(half*(y-top)/math.max(1,bottom-top));for x=cx-span,cx+span do pixel(img,x,y,(x==cx-span or x==cx+span or y==bottom) and ink or c) end end
end
for n,name in ipairs(names) do
 local img=fit(root.."Assets/02.Sprites/"..(bodies[n]==0 and "Slime.png" or "StoneSpirit.png"),64,64)
 local p=colors[n];local base=pc.rgba(p[1],p[2],p[3],255);local light=pc.rgba(math.min(255,p[1]+40),math.min(255,p[2]+40),math.min(255,p[3]+40),255)
 for it in img:pixels() do
  local c=it()
  if alpha(c)<200 then it(0) else
   local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c);local v=(r*.3+g*.59+b*.11)/255
   if v<.18 then it(ink) elseif v>.88 then it(pc.rgba(249,231,212,255)) else
    local shade=v<.4 and .58 or (v<.65 and .82 or 1)
    it(pc.rgba(math.floor(p[1]*shade),math.floor(p[2]*shade),math.floor(p[3]*shade),255))
   end
  end
 end
 local x0,y0,x1,y1=bounds(img);local cx=math.floor((x0+x1)/2);local cy=math.floor((y0+y1)/2)
 local id=n-1
 if id==2 or id==11 then for x=cx-12,cx+12,12 do triangle(img,x,math.max(1,y0-7),4,y0+4,light) end end
 if id==3 then for x=cx-12,cx+12,12 do ellipse(img,x,y0-2,5,4,light) end end
 if id==4 then rect(img,cx-1,math.max(2,y0-8),3,9,ink);ellipse(img,cx,y0-8,4,4,light) end
 if id==5 then rect(img,x0+5,cy-3,math.max(8,x1-x0-9),8,ink);rect(img,x0+7,cy-1,math.max(6,x1-x0-13),4,light) end
 if id==6 then for k=0,4 do rect(img,cx+(k%2==0 and -2 or 1),y0+6+k*4,3,5,light) end end
 if id==7 or id==10 then ellipse(img,x0+2,cy,9,7,light);ellipse(img,x1-2,cy,9,7,light);rect(img,cx-2,cy-8,5,19,ink);rect(img,cx-1,cy-6,3,15,base) end
 if id==8 then rect(img,cx-1,math.max(1,y0-10),3,12,ink);ellipse(img,cx-5,y0-8,7,4,light);ellipse(img,cx+5,y0-10,7,4,base) end
 if id==9 then ellipse(img,cx,y0+1,math.min(25,(x1-x0)/2+3),8,base);for x=cx-12,cx+12,12 do rect(img,x,y0-2,3,3,light) end end
 save(img,name)
end
log:close()
