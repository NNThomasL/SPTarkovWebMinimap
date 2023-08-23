import './style.css';
import ImageLayer from 'ol/layer/Image.js';
import Map from 'ol/Map.js';
import Projection from 'ol/proj/Projection.js';
import {toLonLat, fromLonLat} from 'ol/proj';
import Static from 'ol/source/ImageStatic.js';
import View from 'ol/View.js';
import { getCenter } from 'ol/extent.js';
import { Point } from 'ol/geom';
import { Feature} from 'ol';
import {Icon, Style} from 'ol/style.js';
import {Vector as VectorSource} from 'ol/source.js';
import {Vector as VectorLayer} from 'ol/layer.js';
import MousePosition from 'ol/control/MousePosition';
import {Control, defaults as defaultControls} from 'ol/control.js';
import {defaults as defaultInteractions} from 'ol/interaction/defaults';

import streets_of_tarkov_map_data from './map_data/streets_of_tarkov_map_data.json';
import customs_loot_map_data from './map_data/customs_loot_map_data.json';
import woods_map_data from './map_data/woods_map_data.json';
import lighthouse_loot_map_data from './map_data/lighthouse_loot_map_data.json';
import shoreline_map_data from './map_data/shoreline_map_data.json';
import interchange_map_data from './map_data/interchange_map_modified_data.json';
import reserve_map_data from './map_data/reserve_map_data.json';

let websocket;

let extent = [0, 0, 6539, 4394];
let viewExtent = [-200, -200, 6739, 4594];

const customProjection = new Projection({
  code: 'xkcd-image',
  units: 'pixels',
  extent: extent,
});

let map;
let mapView;
let mapOverlayImage;
let playerMarker;
let currentlyLoadedMap;
let playerVectorLayer;
let playerIconFeature;

let shouldFollowPlayer = false;

let lastGameMap = "";
let lastGameRot = 0;
let lastGamePosX = 0;
let lastGamePosZ = 0;
let lastGamePosY = 0;

const gameMapNamesDict = {
  "bigmap": customs_loot_map_data,
  // "Shoreline": shoreline_map_data,
  // "Interchange": interchange_map_data,
  "TarkovStreets": streets_of_tarkov_map_data,
  "Woods": woods_map_data,
  "Lighthouse": lighthouse_loot_map_data,
  "Shoreline": shoreline_map_data,
  "Interchange": interchange_map_data,
  "RezervBase": reserve_map_data
};


function init() {
  const mousePositionControl = new MousePosition({
    projection: customProjection,
  });

  map = new Map({
    target: 'map',
    controls: defaultControls().extend([new FollowPlayerControl()]), // mousePositionControl
    interactions: defaultInteractions({altShiftDragRotate:false, pinchRotate:false}),
    view: new View({
      projection: customProjection,
      center: getCenter(viewExtent),
      zoom: 1,
      extent: viewExtent,
    })
  });

  map.on('click', function(event) {
    var point = event.coordinate;

    // console.log("event.coordinate:", point);
    console.log(`${lastGamePosX} ${point[0]} ${lastGamePosZ} ${point[1]}`);

    playerIconFeature.getGeometry().setCoordinates(point);

    // DEBUG STUFF BELOW
    lastGameMap = 'TarkovStreets';
    lastGameRot = 90;
    lastGamePosX = -84.273;
    lastGamePosZ = 177.886;
    lastGamePosY = 1.41552567;

    console.log("Last Game Data:", lastGameMap, lastGameRot, lastGamePosX, lastGamePosZ, lastGamePosY);

    let x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].XCoefficients);
    let z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].ZCoefficients);

    // console.log("Polynomial X:", x, "Z:", z);

    // let testCoords = toLonLat(z, x, customProjection);

    // Move the player marker
    playerIconFeature.getGeometry().setCoordinates([x, z]);

    playerIconFeature.getStyle().getImage().setRotation(lastGameRot * (Math.PI / 180));
  });

  // Player marker stuff
  playerIconFeature = new Feature({
    geometry: new Point([0, 0]),
  });
  
  const playerIconStyle = new Style({
    image: new Icon({
      anchor: [0.5, 0.5],
      anchorXUnits: 'fraction',
      anchorYUnits: 'fraction',
      src: '/images/circle_with_arrow.png',
      scale: 0.5
      // width: 10,
      // height: 10
    }),
  });
  
  playerIconFeature.setStyle(playerIconStyle);
  
  const playerVectorSource = new VectorSource({
    features: [playerIconFeature],
  });
  
  playerVectorLayer = new VectorLayer({
    source: playerVectorSource,
  });

  playerVectorLayer.setZIndex(99);

  map.addLayer(playerVectorLayer);

  // Finally attempt to connect
  doConnect();

  changeMap("bigmap");
}

function changeMap(mapName) {
  console.log("Changing map to:", mapName);

  if (mapOverlayImage) {
    map.removeLayer(mapOverlayImage);
  }

  mapOverlayImage = new ImageLayer({
    source: new Static({
      url: `/maps/${gameMapNamesDict[mapName].MapImageFile}`,
      projection: customProjection,
      imageExtent: gameMapNamesDict[mapName].bounds,
    }),
  }),

  map.addLayer(mapOverlayImage);

  viewExtent = [gameMapNamesDict[mapName].bounds[2] * -0.1, gameMapNamesDict[mapName].bounds[2] * -0.1, gameMapNamesDict[mapName].bounds[2] * 1.1, gameMapNamesDict[mapName].bounds[3] * 1.1]; // TODO: Instead of a fixed increase, multiply the normal bound size by ~1.1x

  mapView = new View({
    projection: customProjection,
    center: getCenter(viewExtent),
    showFullExtent: true,
    zoom: gameMapNamesDict[mapName].initialZoom,
    extent: [0, 0, gameMapNamesDict[mapName].bounds[2], gameMapNamesDict[mapName].bounds[3]], //viewExtent,
    rotation: gameMapNamesDict[mapName].MapRotation * (Math.PI / 180),
  });

  mapView.fit(viewExtent, map.getSize()); 

  map.setView(mapView);
  

  currentlyLoadedMap = mapName;
}

function doConnect() {
  websocket = new WebSocket("ws://" + location.host + "/")
  //websocket.onopen = function(evt) { onOpen(evt) }
  //websocket.onclose = function(evt) { onClose(evt) }
  websocket.onmessage = function (evt) { onMessage(evt) }
  websocket.onerror = function (evt) { onError(evt) }
}

function onMessage(evt) {
  let incomingMessageJSON = JSON.parse(evt.data);

  lastGameMap = incomingMessageJSON.mapName;
  lastGameRot = incomingMessageJSON.playerRotationX;
  lastGamePosX = incomingMessageJSON.playerPositionX;
  lastGamePosZ = incomingMessageJSON.playerPositionZ;
  lastGamePosY = incomingMessageJSON.playerPositionY;

  console.log(lastGameMap, lastGameRot, lastGamePosX, lastGamePosZ, lastGamePosY);

  if (currentlyLoadedMap !== lastGameMap) {
    changeMap(lastGameMap);
  }

  let x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].XCoefficients);
  let z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].ZCoefficients);

  if (lastGameMap == "Interchange") {
    if (lastGamePosX > -52 && lastGamePosX < 100 && lastGamePosZ > -180 && lastGamePosZ < 72) {
      if (lastGamePosY < 25) {
        // Parking garage
        //x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].ParkingGarageXCoefficients);
        //z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].ParkingGarageZCoefficients);
      } else if (lastGamePosY > 32) { // Floor 2
        x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].InteriorFloor2XCoefficients);
        z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].InteriorFloor2ZCoefficients);
      }
    }
  }

  // Move the player marker
  playerIconFeature.getGeometry().setCoordinates([x, z]);
  playerIconFeature.getStyle().getImage().setRotation(((gameMapNamesDict[lastGameMap].MapRotation + lastGameRot) * (Math.PI / 180)));

  if (shouldFollowPlayer) mapView.setCenter([x, z]);
}

function onError(evt) {
  websocket.close()
}

function calculatePolynomialValue(x, coefficients) {
  let result = 0;

  result = coefficients[0];

  result += coefficients[1] * x;

  return result;
}

class FollowPlayerControl extends Control {
  /**
   * @param {Object} [opt_options] Control options.
   */
  constructor(opt_options) {
    const options = opt_options || {};

    const button = document.createElement('button');
    button.innerHTML = 'F';

    const element = document.createElement('div');
    element.className = 'follow-player ol-unselectable ol-control';
    element.appendChild(button);

    super({
      element: element,
      target: options.target,
    });

    button.addEventListener('click', this.toggleShouldFollowPlayer.bind(this), false);
  }

  toggleShouldFollowPlayer() {
    shouldFollowPlayer = !shouldFollowPlayer;
    // mapView.setZoom(5);
  }
}

window.addEventListener("load", init, false)
