// Actions shared by the Books (Bookshelf) and Audiobooks (Listenarr) pages.
// "section" is either "books" or "audiobooks", it selects both the redux state and the api route.

export const BOOK_GET_SETTINGS = "get_settings";
export const BOOK_SET_DISABLED_CLIENT = "set_disabled_client";
export const READARR_SET_CLIENT = "set_readarr_client";
export const READARR_LOAD_PATHS = "load_readarr_paths";
export const READARR_SET_PATHS = "set_readarr_paths";
export const READARR_LOAD_PROFILES = "load_readarr_profiles";
export const READARR_SET_PROFILES = "set_readarr_profiles";
export const READARR_LOAD_METADATA_PROFILES = "load_readarr_metadata_profiles";
export const READARR_SET_METADATA_PROFILES = "set_readarr_metadata_profiles";
export const READARR_LOAD_TAGS = "load_readarr_tags";
export const READARR_SET_TAGS = "set_readarr_tags";


export function bookActionType(section, type) {
    return `${section}Clients:${type}`;
};


function createAction(section, type, payload) {
    return {
        type: bookActionType(section, type),
        payload: payload
    };
};


function emptyPromise() {
    return new Promise((resolve, reject) => {
        return { ok: false };
    });
};


function getConnectionBody(readarr) {
    return JSON.stringify({
        "Hostname": readarr.hostname,
        "BaseUrl": readarr.baseUrl,
        "Port": Number(readarr.port),
        "ApiKey": readarr.apiKey,
        "UseSSL": readarr.useSSL,
        "Version": readarr.version
    });
};


export function getBookSettings(section) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch(`../api/${section}`, {
            method: "GET",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            }
        })
            .then(data => data.json())
            .then(data => dispatch(createAction(section, BOOK_GET_SETTINGS, data)));
    };
};


export function saveDisabledBookClient(section) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch(`../api/${section}/disable`, {
            method: "POST",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            },
        })
            .then(data => data.json())
            .then(data => {
                if (data.ok) {
                    dispatch(createAction(section, BOOK_SET_DISABLED_CLIENT));
                    return { ok: true };
                }

                return { ok: false, error: data };
            });
    };
};


function setReadarr(section, dispatch, readarr) {
    dispatch(createAction(section, READARR_SET_CLIENT, { readarr: readarr }));
    return emptyPromise();
};


export function setReadarrConnectionSettings(section, connectionSettings) {
    return (dispatch, getState) => {
        const state = getState();

        return setReadarr(section, dispatch, {
            ...state[section].readarr,
            hostname: connectionSettings.hostname,
            baseUrl: connectionSettings.baseUrl,
            port: connectionSettings.port,
            apiKey: connectionSettings.apiKey,
            useSSL: connectionSettings.useSSL,
            version: connectionSettings.version
        });
    }
};


export function addReadarrCategory(section, category) {
    return (dispatch, getState) => {
        const state = getState();

        return setReadarr(section, dispatch, {
            ...state[section].readarr,
            categories: [...state[section].readarr.categories, category]
        });
    }
};


export function removeReadarrCategory(section, categoryId) {
    return (dispatch, getState) => {
        const state = getState();

        return setReadarr(section, dispatch, {
            ...state[section].readarr,
            categories: state[section].readarr.categories.filter(x => x.id !== categoryId)
        });
    }
};


export function setReadarrCategory(section, categoryId, field, data) {
    return (dispatch, getState) => {
        const state = getState();
        const readarr = state[section].readarr;

        let categories = readarr.categories.map(category => {
            if (category.id !== categoryId)
                return category;

            let newCategory = { ...category };

            if (field === "tags") {
                newCategory.tags = readarr.tags.map(x => x.id).filter(x => data.includes(x));
            } else if (["name", "profileId", "metadataProfileId", "rootFolder"].includes(field)) {
                newCategory[field] = data;
            }

            return newCategory;
        });

        return setReadarr(section, dispatch, {
            ...readarr,
            categories: categories
        });
    }
};


function loadReadarrList(section, forceReload, resource, hasLoadedField, isLoadingField, loadType, setType, wrapResult) {
    return (dispatch, getState) => {
        const state = getState();
        const readarr = state[section].readarr;

        if ((!readarr[hasLoadedField] && !readarr[isLoadingField]) || forceReload) {
            dispatch(createAction(section, loadType, true));

            return fetch(`../api/${section}/client/${resource}`, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${state.user.token}`
                },
                body: getConnectionBody(readarr)
            })
                .then(data => {
                    if (data.status !== 200) {
                        throw new Error("Bad request.");
                    }

                    return data;
                })
                .then(data => data.json())
                .then(data => {
                    dispatch(createAction(section, setType, wrapResult ? { ok: true, data: data } : data));
                    return { ok: true, data: data };
                })
                .catch(err => {
                    dispatch(createAction(section, setType, wrapResult ? { ok: false, data: [] } : []));
                    return { ok: false };
                });
        }

        return emptyPromise();
    };
};


export function loadReadarrRootPaths(section, forceReload) {
    return loadReadarrList(section, forceReload, "rootpath", "hasLoadedPaths", "isLoadingPaths", READARR_LOAD_PATHS, READARR_SET_PATHS, false);
};


export function loadReadarrProfiles(section, forceReload) {
    return loadReadarrList(section, forceReload, "profile", "hasLoadedProfiles", "isLoadingProfiles", READARR_LOAD_PROFILES, READARR_SET_PROFILES, false);
};


export function loadReadarrMetadataProfiles(section, forceReload) {
    return loadReadarrList(section, forceReload, "metadataprofile", "hasLoadedMetadataProfiles", "isLoadingMetadataProfiles", READARR_LOAD_METADATA_PROFILES, READARR_SET_METADATA_PROFILES, false);
};


export function loadReadarrTags(section, forceReload) {
    return loadReadarrList(section, forceReload, "tag", "hasLoadedTags", "isLoadingTags", READARR_LOAD_TAGS, READARR_SET_TAGS, true);
};


export function testReadarrSettings(section, settings) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch(`../api/${section}/client/test`, {
            method: "POST",
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json",
                "Authorization": `Bearer ${state.user.token}`
            },
            body: getConnectionBody(settings)
        })
            .then(data => data.json())
            .then(data => {
                dispatch(loadReadarrProfiles(section, true));
                dispatch(loadReadarrMetadataProfiles(section, true));
                dispatch(loadReadarrRootPaths(section, true));
                dispatch(loadReadarrTags(section, true));

                if (data.ok)
                    return { ok: true };
                return { ok: false, error: data };
            });
    }
};


export function saveReadarrClient(section, saveModel) {
    return (dispatch, getState) => {
        const state = getState();
        const categories = state[section].readarr.categories;

        return fetch(`../api/${section}/client`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            },
            body: JSON.stringify({
                'Hostname': saveModel.readarr.hostname,
                'BaseUrl': saveModel.readarr.baseUrl,
                'Port': Number(saveModel.readarr.port),
                'ApiKey': saveModel.readarr.apiKey,
                'UseSSL': saveModel.readarr.useSSL,
                'Categories': categories,
                'Version': saveModel.readarr.version,
                'SearchNewRequests': saveModel.readarr.searchNewRequests,
                'MonitorNewRequests': saveModel.readarr.monitorNewRequests
            })
        })
            .then(data => data.json())
            .then(data => {
                if (data.ok) {
                    dispatch(createAction(section, READARR_SET_CLIENT, {
                        readarr: {
                            ...state[section].readarr,
                            ...saveModel.readarr,
                            categories: categories
                        },
                        client: saveModel.client
                    }));
                    return { ok: true };
                }

                return { ok: false, error: data };
            });
    }
};
