import './style.css';
import ImageLayer from 'ol/layer/Image.js';
import Map from 'ol/Map.js';
import Projection from 'ol/proj/Projection.js';
import {toLonLat, fromLonLat} from 'ol/proj';
import Static from 'ol/source/ImageStatic.js';
import View from 'ol/View.js';
import { getCenter } from 'ol/extent.js';
import { Point } from 'ol/geom';
import { Feature, Overlay } from 'ol';
import {Icon, Style, Fill} from 'ol/style.js';
import {Vector as VectorSource} from 'ol/source.js';
import {Vector as VectorLayer} from 'ol/layer.js';
import MousePosition from 'ol/control/MousePosition';
import {Control, defaults as defaultControls} from 'ol/control.js';
import {defaults as defaultInteractions} from 'ol/interaction/defaults';
import {pointerMove} from 'ol/events/condition.js';
import Select from 'ol/interaction/Select.js';
import Popup from 'ol-popup/src/ol-popup';

import streets_of_tarkov_map_data from './map_data/streets_of_tarkov_map_data.json';
import customs_loot_map_data from './map_data/customs_loot_map_data.json';
import woods_map_data from './map_data/woods_map_data.json';
import lighthouse_loot_map_data from './map_data/lighthouse_loot_map_data.json';
import shoreline_map_data from './map_data/shoreline_map_data.json';
import interchange_map_data from './map_data/interchange_map_modified_data.json';
import reserve_map_data from './map_data/reserve_map_data.json';
import laboratory_loot_map_data from './map_data/laboratory_loot_map_modified_data.json';
import factory_map_data from './map_data/factory_map_data.json';
import customs_test_map_data from './map_data/customs_test_map_data.json'

let websocket;

let extent = [0, 0, 6539, 4394];
let viewExtent = [-200, -200, 6739, 4594];

/**
 * Sets the projection to a simple 2D pixel system.
 */
const customProjection = new Projection({
  code: 'xkcd-image',
  units: 'pixels',
  extent: extent,
});

/**
 * This popup is used to display quest and cache details along with the QR code for connecting.
 */
const popup = new Popup();

let map;
let mapView;
let mapOverlayImage;
let currentlyLoadedMap;
let playerVectorLayer;
let playerIconFeature;
let airdropVectorLayer;
let airdropVectorSource;
let airdropFeatures = [];
let questVectorLayer;
let questVectorSource;
let questFeatures = [];
let cacheVectorLayer;
let cacheVectorSource;
let cacheFeatures = [];

const airdropIconStyle = new Style({
  image: new Icon({
    anchor: [0.5, 0.5],
    anchorXUnits: 'fraction',
    anchorYUnits: 'fraction',
    src: '/images/airdrop.png',
    scale: 0.5
  })
});

const questIconStyle = new Style({
  image: new Icon({
    anchor: [0.5, 0.5],
    anchorXUnits: 'fraction',
    anchorYUnits: 'fraction',
    src: '/images/check-mark.png',
    scale: 0.5
  })
});

const cacheIconStyle = new Style({
  image: new Icon({
    anchor: [0.5, 0.5],
    anchorXUnits: 'fraction',
    anchorYUnits: 'fraction',
    src: '/images/convergence-target.png',
    scale: 0.5
  })
});

/**
 *  Toggleable setting that tells the map to snap to the moving player marker.
 * @type {boolean}
 */
let shouldFollowPlayer = false;

/**
 * These variables hold the current state of the raid.
 */
let activeRaidCounter = 0;
let lastGameMap = "";
let lastGameRot = 0;
let lastGamePosX = 0;
let lastGamePosZ = 0;
let lastGamePosY = 0;
let lastAirdrops = [];
let lastQuests = [];
let activeQuests = [];


/**
 * An object that maps internal game map names to their corresponding map data.
 */
const gameMapNamesDict = {
  "bigmap": customs_loot_map_data, // Changed to test for now
  "TarkovStreets": streets_of_tarkov_map_data,
  "Woods": woods_map_data,
  "Lighthouse": lighthouse_loot_map_data,
  "Shoreline": shoreline_map_data,
  "Interchange": interchange_map_data,
  "RezervBase": reserve_map_data,
  "laboratory": laboratory_loot_map_data,
  "factory4_day": factory_map_data,
  "factory4_night": factory_map_data
};

/**
 * Initializes the map object with custom controls, interactions, and projection.
 * Also adds overlays and layers for quest, cache, player, airdrop markers, and map image.
 * Finally, attempts to connect to the SPT client and sets the view for the map.
 */
function init() {
  console.log("init() called");

  // Create the map object with the interaction controls and the custom projection that is pixel-based.
  map = new Map({
    target: 'map',
    controls: defaultControls().extend([new FollowPlayerControl(), new QRCodeControl()]),
    interactions: defaultInteractions({altShiftDragRotate:false, pinchRotate:false}),
    view: new View({
      projection: customProjection,
      center: getCenter(viewExtent),
      zoom: 1,
      extent: viewExtent,
    })
  });

  // Add the quest and cache popup overlay
  map.addOverlay(popup);

  // When the map is clicked, check if there is a quest or cache marker and if so, show the correct popup
  map.on('click', function(event) {
    var point = event.coordinate;

    // This is for debugging the polynomial calculations
    // Copy this line of text from the console and put it into CactusPie's EFT Map desktop program in the "Map Creation Mode"'s map positions text box
    console.log(`${lastGamePosX} ${point[0]} ${lastGamePosZ} ${point[1]}`);

    // Check if there is a quest marker at the clicked location
    const questFeatures = map.getFeaturesAtPixel(event.pixel, {
      layerFilter: (layer) => layer === questVectorLayer
    });

    // Check if there is a cache marker at the clicked location
    const cacheFeatures = map.getFeaturesAtPixel(event.pixel, {
      layerFilter: (layer) => layer === cacheVectorLayer
    });
    
    // Show the quest info popup if there is a quest marker
    if (questFeatures.length > 0) {
      popup.show(event.coordinate, `<b>${questFeatures[0].questName}</b></br>${questFeatures[0].questDescription}`);

      return;
    } 
    
    // Show the cache info popup if there is a cache marker
    if (cacheFeatures.length > 0) {
      popup.show(event.coordinate, `<b>Cache</b></br><img src="/images/${cacheFeatures[0].cacheImage}" width="200" height="200"></br>${cacheFeatures[0].cacheHint}`);

      return;
    }
    
    // Otherwise hide the popup
    popup.hide();
  });

  // Player marker icon
  playerIconFeature = new Feature({
    geometry: new Point([0, 0]),
  });
  
  const playerIconStyle = new Style({
    image: new Icon({
      anchor: [0.5, 0.5],
      anchorXUnits: 'fraction',
      anchorYUnits: 'fraction',
      src: '/images/plain-arrow.png',
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

  // Airdrop markers 
  airdropVectorSource = new VectorSource({
    features: [],
  });
  
  airdropVectorLayer = new VectorLayer({
    source: airdropVectorSource,
  });

  airdropVectorLayer.setZIndex(98);

  map.addLayer(airdropVectorLayer);

  // Quest markers
  questVectorSource = new VectorSource({
    features: [],
  });

  questVectorLayer = new VectorLayer({
    source: questVectorSource,
  });

  questVectorLayer.setZIndex(98);

  map.addLayer(questVectorLayer);

  // Cache markers
  cacheVectorSource = new VectorSource({
    features: [],
  });

  cacheVectorLayer = new VectorLayer({
    source: cacheVectorSource,
  });

  cacheVectorLayer.setZIndex(98);

  map.addLayer(cacheVectorLayer);

  // Finally attempt to connect to the SPT client
  doConnect();

  // Load the default "Enter a raid" image
  mapOverlayImage = new ImageLayer({
    source: new Static({
      url: `/maps/enter_a_raid.png`,
      projection: customProjection,
      imageExtent: [0, 0, 600, 400],
    }),
  }),

  map.addLayer(mapOverlayImage);

  // Configure the view for the "Enter a raid" image
  mapView = new View({
    projection: customProjection,
    center: [-100, -100, 700, 500],
    showFullExtent: true,
    zoom: 3,
    extent: [0, 0, 600, 400], //viewExtent,
    rotation: 0,
  });

  map.setView(mapView);
}


/**
 * Changes the map to the specified map name.
 * Removes the old map image, loads the new map image,
 * and sets the view limits to the image bounds +/- 10%.
 *
 * @param {string} mapName - The game internal name of the map to change to.
 */
function changeMap(mapName) {
  console.log("Changing map to:", mapName);

  // Remove the old map image
  if (mapOverlayImage) {
    map.removeLayer(mapOverlayImage);
  }

  // Load the new map image
  mapOverlayImage = new ImageLayer({
    source: new Static({
      url: `/maps/${gameMapNamesDict[mapName].MapImageFile}`,
      projection: customProjection,
      imageExtent: gameMapNamesDict[mapName].bounds,
    }),
  }),

  // Add the image to the map
  map.addLayer(mapOverlayImage);

  // Set the view limits to the image bounds +/- 10%
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


  //TODO: Check if cache icons are even needed anymore after the cache randomization change
  
  // Remove the old cache icons
  cacheFeatures.forEach(item => {
    cacheVectorSource.removeFeature(item);
  });

  cacheFeatures = [];

  currentlyLoadedMap = mapName;
}

/**
 * Adds a new airdrop icon to the map.
 * @param {number} x - The x coordinate of the airdrop icon.
 * @param {number} z - The z coordinate of the airdrop icon.
 */
function addAirdropIcon(x, z) {
  const newAirdropFeature = new Feature({
    geometry: new Point([x, z]),
  });

  newAirdropFeature.setStyle(airdropIconStyle);

  airdropVectorSource.addFeature(newAirdropFeature);

  airdropFeatures.push(newAirdropFeature); // Add the new marker (feature) to the array of features to keep track of it.
}

/**
 * Adds a new quest icon to the map.
 * @param {number} x - The x coordinate of the quest icon.
 * @param {number} z - The z coordinate of the quest icon.
 * @param {string} name - The name of the quest.
 * @param {string} description - The description of the quest.
 */
function addQuestIcon(x, z, name, description) {
  const newQuestFeature = new Feature({
    geometry: new Point([x, z]),
  });

  newQuestFeature.setStyle(questIconStyle);

  newQuestFeature.questName = name;
  newQuestFeature.questDescription = description;

  questVectorSource.addFeature(newQuestFeature);

  questFeatures.push(newQuestFeature); // Add the new marker (feature) to the array of features to keep track of it.
}

// /**
//  * Adds a cache icon to the map at the specified coordinates.
//  * @param {number} x - The x coordinate of the cache icon.
//  * @param {number} z - The z coordinate of the cache icon.
//  * @param {string} image - The image to use for the cache hint image.
//  * @param {string} hint - The hint to display when the cache icon is clicked.
//  */
// function addCacheIcon(x, z, image, hint) {
//   const newCacheFeature = new Feature({
//     geometry: new Point([x, z]),
//   });
//
//   newCacheFeature.setStyle(cacheIconStyle);
//
//   newCacheFeature.cacheImage = image;
//   newCacheFeature.cacheHint = hint;
//
//   cacheVectorSource.addFeature(newCacheFeature);
//
//   cacheFeatures.push(newCacheFeature);
// }

/**
 * Establishes a WebSocket connection to the SPT client mod's web server.
 * @function
 * @returns {void}
 */
function doConnect() {
  try {
    websocket = new WebSocket("ws://" + location.host + "/")
    websocket.onopen = function (evt) {
      onOpen(evt)
    }
    websocket.onclose = function (evt) {
      onClose(evt)
    }
    websocket.onmessage = function (evt) {
      onMessage(evt)
    }
    websocket.onerror = function (evt) {
      onError(evt)
    }
  } catch (e) {
    onError(e);
  }
}

/**
 * Handles incoming messages from the server and updates the game map accordingly.
 * @param {MessageEvent} evt - The message event containing the incoming message data.
 */
function onMessage(evt) {
  let incomingMessageJSON = JSON.parse(evt.data);

  if (incomingMessageJSON.msgType === "mapData") {  
    lastGameMap = incomingMessageJSON.mapName;
    lastGameRot = incomingMessageJSON.playerRotationX;
    lastGamePosX = incomingMessageJSON.playerPositionX;
    lastGamePosZ = incomingMessageJSON.playerPositionZ;
    lastGamePosY = incomingMessageJSON.playerPositionY;
    lastQuests = incomingMessageJSON.quests;

    // TODO: Make a better check for new quest info. Probably should send a request to the server for current quest state.
    if (lastQuests.length !== activeQuests.length) {
      console.log(`lastQuests length != activeQuests length: ${lastQuests.length} vs ${activeQuests.length}`);
      
      questFeatures.forEach(item => {
        questVectorSource.removeFeature(item);
      });

      activeQuests = lastQuests;

      // Add the new ones
      activeQuests.forEach(item => {
        let x = calculatePolynomialValue(item.Where.x, gameMapNamesDict[lastGameMap].XCoefficients);
        let z = calculatePolynomialValue(item.Where.z, gameMapNamesDict[lastGameMap].ZCoefficients);

        addQuestIcon(x, z, item.NameText, item.DescriptionText);
      });
    }

    // Quests
    // Remove the old quest icons on new raid start and create icons for the new quests
    if (activeRaidCounter < incomingMessageJSON.raidCounter && lastGameMap !== "factory4_day" && lastGameMap !== "factory4_night") {
      // Remove the old quest icons
      questFeatures.forEach(item => {
        questVectorSource.removeFeature(item);
      });

      activeQuests = lastQuests;

      // Add the new ones
      activeQuests.forEach(item => {
        let x = calculatePolynomialValue(item.Where.x, gameMapNamesDict[lastGameMap].XCoefficients);
        let z = calculatePolynomialValue(item.Where.z, gameMapNamesDict[lastGameMap].ZCoefficients);

        addQuestIcon(x, z, item.NameText, item.DescriptionText);
      });
    }

    // Airdrops
    // Remove the old airdrop icons on new raid start
    if (activeRaidCounter < incomingMessageJSON.raidCounter) {
      airdropFeatures.forEach(item => {
        airdropVectorSource.removeFeature(item);
      });

      airdropFeatures = [];
    }
    
    // Add the new airdop icon when a new airdrop is detected
    if (airdropFeatures.length < incomingMessageJSON.airdrops.length) {
      // Get all new airdrops by filtering with the existing known ones
      const difference = incomingMessageJSON.airdrops.filter((element) => !airdropFeatures.includes(element));

      difference.forEach(airdrop => {
        let x = calculatePolynomialValue(airdrop.x, gameMapNamesDict[lastGameMap].XCoefficients);
        let z = calculatePolynomialValue(airdrop.z, gameMapNamesDict[lastGameMap].ZCoefficients);

        addAirdropIcon(x, z);
      });
    }

    // Change the map if the player has loaded into a new map
    if (currentlyLoadedMap !== lastGameMap) {
      changeMap(lastGameMap);
    }

    // Get the calculated x and z coordinates for the player marker
    let x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].XCoefficients);
    let z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].ZCoefficients);

    // TODO: Move the multi-floor tracking to a dedicated method.
    // Special cases for maps that have multiple floors
    if (lastGameMap === "Interchange") {
      // Main mall bounding box + Goshan extension
      if ((lastGamePosX < 83 && lastGamePosX > -157.8 && lastGamePosZ < 193.2 && lastGamePosZ > -303.87) || (lastGamePosX < -157.8 && lastGamePosX > -183.4 && lastGamePosZ < 69 && lastGamePosZ > -178.66)) {
        if (lastGamePosY < 23) { // Parking garage
          x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].ParkingGarageXCoefficients);
          z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].ParkingGarageZCoefficients);
        } else if (lastGamePosY > 32) { // Floor 2
          x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].InteriorFloor2XCoefficients);
          z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].InteriorFloor2ZCoefficients);
        }
      }
    } else if (lastGameMap === "laboratory") {
      if (lastGamePosY > 3) {
        x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].Floor2XCoefficients);
        z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].Floor2ZCoefficients);
      } else if (lastGamePosY < -2) {
        x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].TechnicalLevelXCoefficients);
        z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].TechnicalLevelZCoefficients);
      }
    } else if (lastGameMap === "factory4_day" || lastGameMap === "factory4_night") {
      // This map doesn't work
      x = 0;
      z = 0;
    }

    // Move the player marker
    playerIconFeature.getGeometry().setCoordinates([x, z]);
    playerIconFeature.getStyle().getImage().setRotation((gameMapNamesDict[lastGameMap].MapRotation + lastGameRot) * (Math.PI / 180));

    if (shouldFollowPlayer) mapView.setCenter([x, z]);

    activeRaidCounter = incomingMessageJSON.raidCounter;
  } else if (incomingMessageJSON.msgType === "connectAddress") {
    var ipAddress = incomingMessageJSON.ipAddress;
    console.log("Got connect address:", ipAddress);

    var url = `http://${ipAddress}:${location.port}/index.html`;

    popup.show(map.getView().getCenter(), `<div id="qr-code"></div><div>${url}</div>`);

    const qr = new QRCode(document.getElementById("qr-code"), {
      text: url,
      width: 100,
      height: 100,
    });
  } else {
    console.log("Unknown message type:", incomingMessageJSON.msgType);
  }
}

function onOpen(evt) {
  console.log("Opened websocket");
}

function onClose(evt) {
  console.log("Websocket closed");
  console.log(evt);
}

function onError(evt) {
  console.error(evt);
  
  popup.show(map.getView().getCenter(), `<div id="ws-error-message">Failed to connect to SPT-AKI mod.<br>Please check the LogOutput.log in your SPT BepInEx folder.</div>`);
  
  websocket.close();
}

/**
 * Calculates the value of a polynomial for a given x value.
 *
 * @param {number} x - The value of x to evaluate the polynomial.
 * @param {number[]} coefficients - The coefficients of the polynomial. The first element is the constant term, the second
 *                                  element is the coefficient of x, and so on.
 * @return {number} - The result of evaluating the polynomial.
 */
function calculatePolynomialValue(x, coefficients) {
  let result = 0;

  result = coefficients[0];

  result += coefficients[1] * x;

  return result;
}

/**
 * Represents a button control that allows the camera to follow the player marker with updates.
 */
class FollowPlayerControl extends Control {
  constructor(opt_options) {
    const options = opt_options || {};

    const button = document.createElement('button');
    button.className = 'custom-control-button';

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
  }
}

/**
 * Represents a button control that shows the game client computer's connection address as a QR code.
 */
class QRCodeControl extends Control {
  constructor(opt_options) {
    const options = opt_options || {};

    const button = document.createElement('button');
    button.className = 'custom-control-button';

    const element = document.createElement('div');
    element.className = 'qr-code-button ol-unselectable ol-control';
    element.appendChild(button);

    super({
      element: element,
      target: options.target,
    });

    button.addEventListener('click', this.showQRCode.bind(this), false);
  }

  showQRCode() {
    console.log("Showing QR Code");

    // Sends a message to the client mod requesting the computer's local IPV4 address.
    websocket.send(JSON.stringify({type: "get_connect_address"}));
  }
}

// Start the script on load
window.init = init;