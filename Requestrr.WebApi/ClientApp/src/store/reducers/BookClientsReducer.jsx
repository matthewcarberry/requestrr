import {
    bookActionType,
    BOOK_GET_SETTINGS,
    BOOK_SET_DISABLED_CLIENT,
    READARR_SET_CLIENT,
    READARR_LOAD_PATHS,
    READARR_SET_PATHS,
    READARR_LOAD_PROFILES,
    READARR_SET_PROFILES,
    READARR_LOAD_METADATA_PROFILES,
    READARR_SET_METADATA_PROFILES,
    READARR_LOAD_TAGS,
    READARR_SET_TAGS
} from "../actions/BookClientsActions";


// Creates the reducer of the "books" (Bookshelf) or "audiobooks" (Listenarr) section
export default function createBookClientsReducer(section) {
    const is = (action, type) => action.type === bookActionType(section, type);

    const updateReadarr = (state, values) => {
        return {
            ...state,
            readarr: {
                ...state.readarr,
                ...values
            }
        };
    };

    return function BookClientsReducer(state = {}, action) {
        if (is(action, BOOK_GET_SETTINGS)) {
            return {
                ...state,
                client: action.payload.client,
                readarr: {
                    hostname: action.payload.readarr.hostname,
                    baseUrl: action.payload.readarr.baseUrl,
                    port: action.payload.readarr.port,
                    apiKey: action.payload.readarr.apiKey,
                    useSSL: action.payload.readarr.useSSL,
                    categories: action.payload.readarr.categories,
                    searchNewRequests: action.payload.readarr.searchNewRequests,
                    monitorNewRequests: action.payload.readarr.monitorNewRequests,
                    version: action.payload.readarr.version,
                    isLoadingPaths: false,
                    hasLoadedPaths: false,
                    arePathsValid: false,
                    paths: [],
                    isLoadingProfiles: false,
                    hasLoadedProfiles: false,
                    areProfilesValid: false,
                    profiles: [],
                    isLoadingMetadataProfiles: false,
                    hasLoadedMetadataProfiles: false,
                    areMetadataProfilesValid: false,
                    metadataProfiles: [],
                    isLoadingTags: false,
                    hasLoadedTags: false,
                    areTagsValid: false,
                    tags: []
                },
                otherCategories: action.payload.otherCategories
            };
        } else if (is(action, BOOK_SET_DISABLED_CLIENT)) {
            return {
                ...state,
                client: "Disabled"
            };
        } else if (is(action, READARR_SET_CLIENT)) {
            return {
                ...state,
                readarr: action.payload.readarr,
                client: action.payload.client ?? state.client
            };
        } else if (is(action, READARR_LOAD_PATHS)) {
            return updateReadarr(state, { isLoadingPaths: action.payload });
        } else if (is(action, READARR_SET_PATHS)) {
            return updateReadarr(state, { isLoadingPaths: false, hasLoadedPaths: true, arePathsValid: action.payload.length > 0, paths: action.payload });
        } else if (is(action, READARR_LOAD_PROFILES)) {
            return updateReadarr(state, { isLoadingProfiles: action.payload });
        } else if (is(action, READARR_SET_PROFILES)) {
            return updateReadarr(state, { isLoadingProfiles: false, hasLoadedProfiles: true, areProfilesValid: action.payload.length > 0, profiles: action.payload });
        } else if (is(action, READARR_LOAD_METADATA_PROFILES)) {
            return updateReadarr(state, { isLoadingMetadataProfiles: action.payload });
        } else if (is(action, READARR_SET_METADATA_PROFILES)) {
            return updateReadarr(state, { isLoadingMetadataProfiles: false, hasLoadedMetadataProfiles: true, areMetadataProfilesValid: action.payload.length > 0, metadataProfiles: action.payload });
        } else if (is(action, READARR_LOAD_TAGS)) {
            return updateReadarr(state, { isLoadingTags: action.payload });
        } else if (is(action, READARR_SET_TAGS)) {
            return updateReadarr(state, { isLoadingTags: false, hasLoadedTags: true, areTagsValid: action.payload.ok, tags: action.payload.data });
        }

        return { ...state };
    };
}
