module PissedOffOwls

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Audio
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Input.Touch
open Microsoft.Xna.Framework.Content
open FarseerPhysics
open FarseerPhysics.Common
open FarseerPhysics.Dynamics
open FarseerPhysics.Factories

let physicsScale = 0.1f

type ActorType = Player | Enemy | Prop | Ignore
type Shape = Box | Circle
type PhysicsType = Static | Dynamic | None
type Position = Position of float32 * float32

type Physics = 
| NonPhysics of Vector2
| Physics of Body

type Actor = //TODO make it so you dont specify a shape if you are ignoring
| Actor of string * Position * Shape * ActorType * PhysicsType

let isAlive = function
| Physics(body) -> not body.IsDisposed
| NonPhysics(position) -> true
                                                                                                                                                                                 
let world = World(Vector2(0.0f,-10.0f))
       
let buildBody (position:Vector2) isStatic width height shape actorType =                                                                                                                                                                                                                                                                                                                                                                                                                                                                            
    let width, height = float32 width, float32 height
    let body = match shape with
               | Circle -> BodyFactory.CreateCircle(world, physicsScale * width / 2.0f, 1.f)
               | Box -> BodyFactory.CreateRectangle(world, physicsScale * width, physicsScale * height, 1.f)
    body.Position <- position * physicsScale 
    body.UserData <- actorType  
    body.IsStatic <- isStatic
    body.Restitution <- 0.2f
    body.add_OnCollision(fun f f' contact -> 
        let data = f.Body.UserData :?> ActorType
        let data' = f'.Body.UserData :?> ActorType
        match data, data' with //Maybe have a better way to kill?
        | Player, Enemy -> f'.Body.Dispose()
        | Enemy, Player -> f.Body.Dispose()
        | Enemy, Prop -> f.Body.Dispose()
        | Prop, Enemy -> f'.Body.Dispose()
        | _ -> ()
        true)    
    body
        
let buildPhysics width height shape x y actorType = function
| Dynamic -> Physics(buildBody (Vector2(x,y)) false width height shape actorType)
| Static -> Physics(buildBody (Vector2(x,y)) true width height shape actorType)
| None -> NonPhysics(Vector2(x,y))                                                                                                                                                                                                                                                                                                                                                                                                                                                                       

type PissedOffOwl(width, height) as this = 
    inherit Game()
    let screenWidth, screenHeight = 320.f, 240.f
    let scaleMatrix = Matrix.CreateScale( width / screenWidth, height / screenHeight, 1.0f)
    
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>
    
    let graphics = new GraphicsDeviceManager(this)
    do graphics.IsFullScreen <- true
    do graphics.SupportedOrientations <- DisplayOrientation.LandscapeLeft ||| DisplayOrientation.LandscapeRight
    do this.Content.RootDirectory <- "Content"
    let getPosition physics actorType = 
        match actorType, physics with
        | Player, Physics(body) -> 
            let touches = TouchPanel.GetState()
            if touches.Count > 0 then
                let touch = touches.[0]
                match touch.State with
                | TouchLocationState.Moved -> 
                    let touchPosition = Vector2(touch.Position.X * screenWidth / width, 
                                                (height - touch.Position.Y) *  screenHeight / height)
                    body.Position <- touchPosition * physicsScale
                | TouchLocationState.Released -> 
                    //SUCKY HARDCODED VALUE for relative launch point!!!
                    body.ResetDynamics()
                    let diff = Vector2(70.f,64.f) - (body.Position /  physicsScale)
                    body.ApplyForce (diff * 50.0f)
                | _ -> () //Dont care
            body.Position / physicsScale, body.Rotation
        | _, Physics(body) -> body.Position / physicsScale, body.Rotation
        | _, NonPhysics(position) -> position, 0.0f

    let draw (texture:Texture2D) (position:Vector2) rotation = 
        let position = Vector2(position.X, screenHeight - position.Y)
        let origin = Vector2(float32 texture.Width / 2.0f, float32 texture.Height / 2.0f)
        spriteBatch.Draw(texture, position, Nullable(), Color.White, -rotation, origin, 1.0f, SpriteEffects.None, 0.0f)
    
    let drawActor (actor: Lazy<_>) = 
        let texture, physics, actorType = actor.Value
        match isAlive physics with
        | true -> let position, rotation = getPosition physics actorType
                  draw texture position rotation
        | _ -> ()
        
    let createActor (Actor(name,Position(x,y), shape, actorType, physicsType)) = 
        lazy 
            let texture = this.Content.Load<Texture2D> name  
            let physics = buildPhysics texture.Width texture.Height shape x y actorType physicsType
            texture, physics, actorType
        
    let player = Actor("Owl",Position(30.f,8.f), Circle, Player, Dynamic) |> createActor
    let level =  [ Actor("Background", Position(160.f,120.f), Box, Ignore, None)
                   Actor("Catapult", Position(70.f,32.f), Box, Ignore, None)
                   Actor("Mouse", Position(185.f,15.f), Circle, Enemy, Dynamic)
                   Actor("Mouse", Position(285.f, 15.f), Circle, Enemy, Dynamic)
                   Actor("Tower", Position(225.f, 64.f), Box, Prop, Dynamic) ]
                 @[ for i in 0..20 do yield Actor("Brick", Position(float32(i * 16), 0.f), Box, Ignore, Static) ]
                 |>List.map createActor
            
    override __.Initialize() = spriteBatch <- new SpriteBatch(graphics.GraphicsDevice) 
    override __.Update time = world.Step 0.016f
    override __.Draw time = 
        graphics.GraphicsDevice.Clear(Color.CornflowerBlue)
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, scaleMatrix)
        level|>List.iter drawActor
        drawActor player
        spriteBatch.End()
