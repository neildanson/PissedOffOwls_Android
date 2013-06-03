namespace PissedOffOwls_Android

open PissedOffOwls
open System
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Microsoft.Xna.Framework

[<Activity (Label = "PissedOffOwls_Android", MainLauncher = true)>]
type MainActivity () =
    inherit AndroidGameActivity ()

  
    override this.OnCreate (bundle) =

        base.OnCreate (bundle)
        PissedOffOwl.Activity <- this
        let game = new PissedOffOwl(100.0f, 100.0f)
        game.Run()

