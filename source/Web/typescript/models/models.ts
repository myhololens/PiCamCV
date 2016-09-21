﻿module App {

    export enum Direction {
        Unknown= 0,
        Pan,
        Tilt
    }

    export interface IPanTiltSetting {
        panPercent?: number;
        tiltPercent?: number;
    }

    export interface IBrowserServer {
        movePanTilt(plane: Direction, units: number): JQueryPromise<void>;
        hello(message:string): JQueryPromise<void>;
    }
    export interface IBrowserClient {
        imageReady: (data: string) => void;
        screenWriteLine: (message: string) => void;
        toast: (message: string) => void;
        screenClear: () => void;
    }

    export interface IBrowserHubProxy {
        client: IBrowserClient;
        server: IBrowserServer;
    }

    export interface ISignalR {
        browserHub: IBrowserHubProxy ;
    }
}