import {
    AfterContentInit,
    Component,
    Input,
    OnChanges,
    OnDestroy,
    OnInit,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PostDTO } from 'src/DTO/PostDTO/PostDTO';
import { PostService } from '../services/post/post.service';
import { GroupService } from '../services/group/group.service';
import { DateFormatterService } from '../services/date/date-formatter.service';

@Component({
    selector: 'app-post',
    templateUrl: './post.component.html',
    styleUrls: ['./post.component.css'],
})
export class PostComponent implements AfterContentInit {
    constructor(
        private postService: PostService,
        private router: Router,
        private dateFormatter: DateFormatterService
    ) {}

    ngAfterContentInit(): void {
        this.upVoted = this.post.voteValue == 1;
        this.downVoted = this.post.voteValue == 2;
    }

    @Input() canDelete : boolean = false

    @Input() displayGroup: boolean = true;

    @Input() post: PostDTO = {
        id: 0,
        jwt: '',
        title: '',
        content: '',
        groupId: 0,
        indexedImg: 0,
        authorName: '',
        groupName: '',
        likeCount: 0,
        postDate: new Date(),
        authorPhoto: 0,
        voteValue: 0,
        isAuthor: false,
        canDelete: false
    };

    upVoted: boolean = false;
    downVoted: boolean = false;

    postLink = (): string => {
        return '/group/' + this.post.groupName + '/post/' + this.post.id;
    };

    imgUrl = (): string => {
        if (this.post.authorPhoto == 0 || this.post.authorPhoto == undefined) {
            return '../assets/image/avatar-placeholder.png';
        }
        return 'http://localhost:5038/img/' + this.post.authorPhoto;
    };

    like = (): void => {

        let jwt = sessionStorage.getItem('jwtSession') ?? '';
        let postId = this.post.id;

        if (this.post.voteValue == 1) {
            this.postService
                .unvotePost({ jwt, postId, value: false })
                .subscribe((res) => {
                    window.location.reload();
                });
        } else {
            this.postService
                .votePost({ jwt, postId, value: true })
                .subscribe((res) => {
                    window.location.reload();
                });
        }
    };

    dislike = (): void => {
        let jwt = sessionStorage.getItem('jwtSession') ?? '';
        let postId = this.post.id;

        if (this.post.voteValue == 2) {
            this.postService
                .unvotePost({ jwt, postId, value: false })
                .subscribe((res) => {
                    window.location.reload();
                });
            this.ngAfterContentInit();
            return;
        } else {
            this.postService
                .votePost({ jwt, postId, value: false })
                .subscribe((res) => {
                    window.location.reload();
                });
        }
    };

    formatedDate = (): string => {
        return this.dateFormatter.formatDate(this.post.postDate);
    };

    deletePost() {
        if(!confirm("Tem certeza que deseja deletar este post ?"))
            return

        this.post.jwt = sessionStorage.getItem("jwtSession") ?? ""

        this.postService.deletePost(this.post)
            .subscribe(res => {
                location.reload()
            })
    }
}
